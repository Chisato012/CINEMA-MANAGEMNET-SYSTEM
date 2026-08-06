using System.Security.Claims;
using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Management.Controllers;

public sealed class GoogleAuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly UserSignInService _userSignInService;
    private readonly ILogger<GoogleAuthController> _logger;

    public GoogleAuthController(
        ApplicationDbContext context,
        IConfiguration configuration,
        UserSignInService userSignInService,
        ILogger<GoogleAuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _userSignInService = userSignInService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!IsGoogleLoginEnabled())
        {
            TempData["AlertError"] = "Dang nhap Google chua duoc cau hinh.";
            return RedirectToAction("Login", "Account");
        }

        var redirectUrl = Url.Action(
            nameof(Callback),
            "GoogleAuth",
            new { returnUrl });

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUrl },
            GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> Callback(
        string? returnUrl,
        string? remoteError,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            TempData["AlertError"] = "Dang nhap Google that bai. Vui long thu lai.";
            return RedirectToAction("Login", "Account");
        }

        var externalResult = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = externalResult.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            TempData["AlertError"] = "Khong doc duoc thong tin dang nhap Google.";
            return RedirectToAction("Login", "Account");
        }

        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = NormalizeEmail(principal.FindFirstValue(ClaimTypes.Email));
        var fullName = principal.FindFirstValue(ClaimTypes.Name)
                       ?? email.Split('@')[0];

        if (string.IsNullOrWhiteSpace(providerKey)
            || string.IsNullOrWhiteSpace(email))
        {
            await ClearExternalCookieAsync();
            TempData["AlertError"] = "Tai khoan Google khong cung cap du email.";
            return RedirectToAction("Login", "Account");
        }

        if (!IsGoogleEmailVerified(principal))
        {
            await ClearExternalCookieAsync();
            TempData["AlertError"] = "Email Google chua duoc xac minh.";
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.ExternalProvider == GoogleDefaults.AuthenticationScheme
                     && u.ExternalProviderKey == providerKey,
                cancellationToken);

        if (user == null)
        {
            user = await FindUserByNormalizedEmailAsync(email, cancellationToken);

            if (user == null)
            {
                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = string.Empty,
                    Role = "KhachHang",
                    Status = true,
                    EmailConfirmed = true,
                    ExternalProvider = GoogleDefaults.AuthenticationScheme,
                    ExternalProviderKey = providerKey
                };

                _context.Users.Add(user);
            }
            else
            {
                user.EmailConfirmed = true;
                user.ExternalProvider = GoogleDefaults.AuthenticationScheme;
                user.ExternalProviderKey = providerKey;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Google login failed while linking provider for email {Email}.",
                    email);
                await ClearExternalCookieAsync();
                TempData["AlertError"] = "Khong lien ket duoc tai khoan Google. Vui long thu lai.";
                return RedirectToAction("Login", "Account");
            }
        }

        if (!user.Status)
        {
            await ClearExternalCookieAsync();
            TempData["AlertError"] = "Tai khoan cua ban da bi khoa.";
            return RedirectToAction("Login", "Account");
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _userSignInService.SignInAsync(HttpContext, user, rememberMe: true);
        TempData["AlertSuccess"] = $"Dang nhap Google thanh cong. Xin chao {user.FullName}.";

        return RedirectAfterSignIn(user, returnUrl);
    }

    private Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return _context.Users.FirstOrDefaultAsync(
            u => u.Email.Trim().ToLower() == normalizedEmail,
            cancellationToken);
    }

    private IActionResult RedirectAfterSignIn(User user, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return user.Role switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "Staff" => RedirectToAction("Index", "Staff"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    private bool IsGoogleLoginEnabled()
    {
        return !string.IsNullOrWhiteSpace(
                   _configuration["Authentication:Google:ClientId"])
               && !string.IsNullOrWhiteSpace(
                   _configuration["Authentication:Google:ClientSecret"]);
    }

    private static bool IsGoogleEmailVerified(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("urn:google:email_verified")
                    ?? principal.FindFirstValue("email_verified");

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
               || value == "1";
    }

    private async Task ClearExternalCookieAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();
    }
}
