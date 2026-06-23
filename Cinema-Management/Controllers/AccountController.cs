using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Management.Controllers;

public class AccountController : Controller
{
    private const string GoogleProvider = "Google";
    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ResendConfirmationCooldown = TimeSpan.FromMinutes(2);

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        SetTurnstileSiteKey();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, CancellationToken cancellationToken)
    {
        SetTurnstileSiteKey();

        model.CaptchaToken = Request.Form["cf-turnstile-response"].ToString();
        ModelState.Remove(nameof(LoginRequest.CaptchaToken));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await IsTurnstileValidAsync(cancellationToken))
        {
            ViewBag.CaptchaError = "Vui lòng xác minh captcha.";
            TempData["AlertError"] = "Xác minh CAPTCHA thất bại. Vui lòng thử lại.";
            return View(model);
        }

        var email = NormalizeEmail(model.Email);
        var user = await FindUserByNormalizedEmailAsync(email, cancellationToken);

        if (user == null || !IsPasswordValid(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Sai email hoặc mật khẩu");
            TempData["AlertError"] = "Sai email hoặc mật khẩu. Vui lòng thử lại.";
            return View(model);
        }

        if (!user.Status)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa");
            TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
            return View(model);
        }

        if (!user.EmailConfirmed)
        {
            ViewBag.UnconfirmedEmail = user.Email;
            ModelState.AddModelError(string.Empty, "Email của bạn chưa được xác nhận.");
            TempData["AlertError"] = "Email của bạn chưa được xác nhận.";
            return View(model);
        }

        SignInWithSession(user);

        var role = GetUserRole(user);
        TempData["AlertSuccess"] = $"Đăng nhập thành công! Xin chào {user.FullName} (Role: {role})";

        return RedirectByRole(role);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginApi(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var email = NormalizeEmail(request.Email);
        var user = await FindUserByNormalizedEmailAsync(email, cancellationToken);

        if (user == null || !IsPasswordValid(request.Password, user.PasswordHash))
        {
            return Unauthorized("Sai email hoặc mật khẩu");
        }

        if (!user.Status)
        {
            return Unauthorized("Tài khoản đã bị khóa");
        }

        if (!user.EmailConfirmed)
        {
            return Unauthorized("Email của bạn chưa được xác nhận");
        }

        return Ok(new
        {
            message = "Đăng nhập thành công",
            user = new
            {
                user.UserID,
                user.FullName,
                user.Email
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["AlertSuccess"] = "Bạn đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        AuthViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = NormalizeEmail(model.Email);
        var emailExists = await UserEmailExistsAsync(email, cancellationToken);

        if (emailExists)
        {
            AddDuplicateAccountError();
            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            PhoneNumber = model.PhoneNumber.Trim(),
            DOB = model.DateOfBirth,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = "KhachHang",
            Status = true,
            EmailConfirmed = false
        };

        var verificationToken = CreateEmailVerificationToken(user);
        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateAccountError(exception))
        {
            AddDuplicateAccountError();
            return View(model);
        }

        await SendConfirmationEmailAsync(user, verificationToken, cancellationToken);

        return RedirectToAction(nameof(RegisterPending), new { email = user.Email });
    }

    [HttpGet]
    public IActionResult RegisterPending(string? email)
    {
        ViewBag.Email = NormalizeEmail(email);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(token))
        {
            return ConfirmEmailFailedView(
                normalizedEmail,
                "Liên kết xác nhận không hợp lệ.");
        }

        var user = await FindUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user == null)
        {
            return ConfirmEmailFailedView(
                normalizedEmail,
                "Liên kết xác nhận không hợp lệ hoặc đã hết hạn.");
        }

        if (user.EmailConfirmed)
        {
            return View("ConfirmEmailSuccess");
        }

        if (string.IsNullOrWhiteSpace(user.EmailVerificationTokenHash)
            || !user.EmailVerificationTokenExpiresAt.HasValue)
        {
            return ConfirmEmailFailedView(
                normalizedEmail,
                "Tài khoản chưa có mã xác nhận hợp lệ. Vui lòng gửi lại email xác nhận.");
        }

        if (user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return ConfirmEmailFailedView(
                normalizedEmail,
                "Liên kết xác nhận đã hết hạn. Vui lòng gửi lại email xác nhận.");
        }

        if (!IsTokenHashValid(token, user.EmailVerificationTokenHash))
        {
            return ConfirmEmailFailedView(
                normalizedEmail,
                "Liên kết xác nhận không hợp lệ.");
        }

        user.EmailConfirmed = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        user.EmailVerificationLastSentAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        return View("ConfirmEmailSuccess");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmationEmail(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        TempData["AlertSuccess"] =
            "Nếu email hợp lệ và chưa xác nhận, chúng tôi sẽ gửi liên kết xác nhận mới.";

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return RedirectToAction(nameof(RegisterPending));
        }

        var user = await FindUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user == null || user.EmailConfirmed || !user.Status)
        {
            return RedirectToAction(nameof(RegisterPending), new { email = normalizedEmail });
        }

        var now = DateTime.UtcNow;
        if (user.EmailVerificationLastSentAt.HasValue
            && now - user.EmailVerificationLastSentAt.Value < ResendConfirmationCooldown)
        {
            return RedirectToAction(nameof(RegisterPending), new { email = normalizedEmail });
        }

        var verificationToken = CreateEmailVerificationToken(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateAccountError(exception))
        {
            _logger.LogWarning(exception, "Could not resend confirmation email because of duplicate user data.");
            return RedirectToAction(nameof(RegisterPending), new { email = normalizedEmail });
        }

        await SendConfirmationEmailAsync(user, verificationToken, cancellationToken);

        return RedirectToAction(nameof(RegisterPending), new { email = normalizedEmail });
    }

    [HttpGet]
    public IActionResult GoogleLogin()
    {
        if (string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"])
            || string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]))
        {
            TempData["AlertError"] = "Google Login chưa được cấu hình.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account");
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
            TempData["AlertError"] = "Đăng nhập Google thất bại hoặc đã bị hủy.";
            return RedirectToAction(nameof(Login));
        }

        var principal = authenticateResult.Principal;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = NormalizeEmail(principal.FindFirstValue(ClaimTypes.Email));
        var fullName = principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var emailVerified = IsGoogleEmailVerified(principal);

        if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(email))
        {
            TempData["AlertError"] = "Google không trả về đủ thông tin tài khoản.";
            return RedirectToAction(nameof(Login));
        }

        if (!emailVerified)
        {
            TempData["AlertError"] = "Google chưa xác minh email của tài khoản này.";
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.ExternalProvider == GoogleProvider
                     && u.ExternalProviderKey == googleId,
                cancellationToken);

        var shouldSave = false;
        if (user == null)
        {
            var existingEmailUser = await FindUserByNormalizedEmailAsync(email, cancellationToken);

            if (existingEmailUser != null)
            {
                if (!existingEmailUser.Status)
                {
                    TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
                    return RedirectToAction(nameof(Login));
                }

                if (HasDifferentExternalProviderKey(existingEmailUser, googleId))
                {
                    TempData["AlertError"] = "Email này đã được liên kết với nhà cung cấp đăng nhập khác.";
                    return RedirectToAction(nameof(Login));
                }

                LinkGoogleAccount(existingEmailUser, googleId);
                user = existingEmailUser;
                shouldSave = true;
            }
            else
            {
                user = new User
                {
                    FullName = string.IsNullOrWhiteSpace(fullName)
                        ? email
                        : fullName.Trim(),

                    Email = email,
                    PasswordHash = null,

                    // Gmail mới luôn là khách hàng
                    Role = "KhachHang",

                    Status = true,
                    EmailConfirmed = true,
                    ExternalProvider = GoogleProvider,
                    ExternalProviderKey = googleId
                };

                _context.Users.Add(user);
                shouldSave = true;
            }

            if (shouldSave)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException exception) when (IsDuplicateAccountError(exception))
                {
                    var linkedUser = await TryLinkExistingUserAfterGoogleDuplicateAsync(
                        user,
                        email,
                        googleId,
                        cancellationToken);

                    if (linkedUser == null)
                    {
                        TempData["AlertError"] = "Không thể liên kết tài khoản Google này.";
                        return RedirectToAction(nameof(Login));
                    }

                    user = linkedUser;
                }
            }
        }

        if (!user.Status)
        {
            TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
            return RedirectToAction(nameof(Login));
        }

        if (!user.EmailConfirmed)
        {
            TempData["AlertError"] = "Email của bạn chưa được xác nhận.";
            return RedirectToAction(nameof(Login));
        }

        SignInWithSession(user);

        var role = GetUserRole(user);
        TempData["AlertSuccess"] = $"Đăng nhập Google thành công! Xin chào {user.FullName}";

        return RedirectByRole(role);
    }

    [HttpGet]
    public IActionResult GoogleLoginFailed()
    {
        TempData["AlertError"] = "Đăng nhập Google đã bị hủy hoặc thất bại.";
        return RedirectToAction(nameof(Login));
    }

    private IActionResult ConfirmEmailFailedView(string email, string message)
    {
        ViewBag.Email = email;
        ViewBag.Message = message;
        return View("ConfirmEmailFailed");
    }

    private async Task SendConfirmationEmailAsync(
        User user,
        string verificationToken,
        CancellationToken cancellationToken)
    {
        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { email = user.Email, token = verificationToken },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(confirmationLink))
        {
            _logger.LogWarning("Could not create email confirmation URL for user {UserID}.", user.UserID);
            return;
        }

        var safeFullName = WebUtility.HtmlEncode(user.FullName);
        var safeLink = WebUtility.HtmlEncode(confirmationLink);
        var htmlBody = $"""
            <!doctype html>
            <html>
            <body style="margin:0;background:#121212;font-family:Arial,sans-serif;color:#f5f5f5;">
                <div style="max-width:560px;margin:0 auto;padding:32px 24px;">
                    <h1 style="color:#D4A373;margin:0 0 12px;">COSMOS Cinema</h1>
                    <p>Chào {safeFullName},</p>
                    <p>Cảm ơn bạn đã đăng ký tài khoản COSMOS Cinema. Vui lòng xác nhận email để hoàn tất đăng ký.</p>
                    <p style="margin:28px 0;">
                        <a href="{safeLink}" style="background:#A67B5B;color:#ffffff;text-decoration:none;padding:14px 22px;border-radius:8px;font-weight:bold;display:inline-block;">
                            Xác nhận tài khoản
                        </a>
                    </p>
                    <p>Liên kết này có hiệu lực trong 30 phút.</p>
                    <p style="color:#aaa;font-size:13px;">Nếu bạn không tạo tài khoản, vui lòng bỏ qua email này.</p>
                </div>
            </body>
            </html>
            """;

        await _emailService.SendEmailAsync(
            user.Email,
            "Xác nhận tài khoản COSMOS Cinema",
            htmlBody,
            cancellationToken);
    }

    private void SignInWithSession(User user)
    {
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserRole", GetUserRole(user));
        HttpContext.Session.SetInt32("UserID", user.UserID);
    }

    private static string GetUserRole(User user)
    {
        return string.IsNullOrWhiteSpace(user.Role) ? "Customer" : user.Role;
    }

    private IActionResult RedirectByRole(string role)
    {
        return role switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "Staff" => RedirectToAction("Index", "Staff"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    private Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return _context.Users
            .FirstOrDefaultAsync(
                u => u.Email.Trim().ToLower() == normalizedEmail,
                cancellationToken);
    }

    private Task<bool> UserEmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return _context.Users
            .AnyAsync(
                u => u.Email.Trim().ToLower() == normalizedEmail,
                cancellationToken);
    }

    private static bool HasDifferentExternalProviderKey(User user, string googleId)
    {
        return !string.IsNullOrWhiteSpace(user.ExternalProviderKey)
               && !string.Equals(user.ExternalProviderKey, googleId, StringComparison.Ordinal);
    }

    private static void LinkGoogleAccount(User user, string googleId)
    {
        user.ExternalProvider = GoogleProvider;
        user.ExternalProviderKey = googleId;
        user.EmailConfirmed = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        user.EmailVerificationLastSentAt = null;
    }

    private async Task<User?> TryLinkExistingUserAfterGoogleDuplicateAsync(
        User pendingUser,
        string email,
        string googleId,
        CancellationToken cancellationToken)
    {
        var entry = _context.Entry(pendingUser);
        entry.State = entry.State == EntityState.Added
            ? EntityState.Detached
            : EntityState.Unchanged;

        var existingUser = await FindUserByNormalizedEmailAsync(email, cancellationToken);

        if (existingUser == null
            || !existingUser.Status
            || HasDifferentExternalProviderKey(existingUser, googleId))
        {
            return null;
        }

        LinkGoogleAccount(existingUser, googleId);
        await _context.SaveChangesAsync(cancellationToken);

        return existingUser;
    }

    private void AddDuplicateAccountError()
    {
        ModelState.AddModelError(nameof(AuthViewModel.Email), "Tài khoản đã có");
    }

    private static bool IsDuplicateAccountError(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
               && sqlException.Errors
                   .Cast<SqlError>()
                   .Any(error => error.Number is 2601 or 2627);
    }

    private void SetTurnstileSiteKey()
    {
        ViewBag.TurnstileSiteKey = _configuration["CloudflareTurnstile:SiteKey"];
    }

    private async Task<bool> IsTurnstileValidAsync(CancellationToken cancellationToken)
    {
        var token = Request.Form["cf-turnstile-response"].ToString();
        var secretKey = _configuration["CloudflareTurnstile:SecretKey"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secretKey))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token,
                    ["remoteip"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                }),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(
                cancellationToken);
            return result?.Success == true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Cloudflare Turnstile request failed.");
            return false;
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "Cloudflare Turnstile request timed out.");
            return false;
        }
    }

    private static bool IsPasswordValid(string password, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();
    }

    private static string CreateEmailVerificationToken(User user)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        user.EmailVerificationTokenHash = HashToken(token);
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.Add(EmailVerificationLifetime);
        user.EmailVerificationLastSentAt = DateTime.UtcNow;

        return token;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static bool IsTokenHashValid(string token, string storedHash)
    {
        var incomingHash = HashToken(token);
        var incomingBytes = Encoding.UTF8.GetBytes(incomingHash);
        var storedBytes = Encoding.UTF8.GetBytes(storedHash);

        return incomingBytes.Length == storedBytes.Length
               && CryptographicOperations.FixedTimeEquals(incomingBytes, storedBytes);
    }

    private static bool IsGoogleEmailVerified(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("urn:google:email_verified");
        return bool.TryParse(value, out var emailVerified) && emailVerified;
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
