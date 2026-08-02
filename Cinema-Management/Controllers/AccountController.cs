using System.Security.Claims;
using Cinema_Management.Data;
using Cinema_Management.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Management.Controllers;
// BE Bắc
public class AccountController : Controller
{
    private static readonly HashSet<string> DevelopmentPasswordlessEmails =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "khach@gmail.com",
            "staff@gmail.com",
            "admin@gmail.com"
        };
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IWebHostEnvironment environment,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        _environment = environment;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, CancellationToken cancellationToken)
    {
        var normalizedEmail = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var isDevelopmentPasswordlessLogin =
            _environment.IsDevelopment()
            && DevelopmentPasswordlessEmails.Contains(normalizedEmail);

        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "Development passwordless login attempted. Matched allowlist: {MatchedAllowlist}.",
                DevelopmentPasswordlessEmails.Contains(normalizedEmail));
        }

        if (isDevelopmentPasswordlessLogin)
        {
            ModelState.Remove(nameof(LoginRequest.Password));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
            return View(model);
        }

        var user = await FindUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (isDevelopmentPasswordlessLogin)
        {
            _logger.LogInformation(
                "Development passwordless login database lookup. User found: {UserFound}. Database role: {DatabaseRole}.",
                user != null,
                user?.Role ?? "(none)");

            if (user == null)
            {
                _logger.LogWarning(
                    "Development passwordless login failed because the allowlisted user was not found.");
            }
        }

        if (user == null || (!isDevelopmentPasswordlessLogin && !IsPasswordValid(model.Password, user.PasswordHash)))
        {
            if (isDevelopmentPasswordlessLogin)
            {
                _logger.LogWarning(
                    "Development passwordless login failed. Matched allowlist: {MatchedAllowlist}. User found: {UserFound}. Database role: {DatabaseRole}.",
                    DevelopmentPasswordlessEmails.Contains(normalizedEmail),
                    user != null,
                    user?.Role ?? "(none)");
            }

            ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
            ModelState.AddModelError(string.Empty, "Sai email hoặc mật khẩu");
            TempData["AlertError"] = "Sai email hoặc mật khẩu. Vui lòng thử lại.";
            return View(model);
        }

        if (!user.Status)
        {
            if (isDevelopmentPasswordlessLogin)
            {
                _logger.LogWarning(
                    "Development passwordless login failed because the account is inactive. Database role: {DatabaseRole}.",
                    user.Role);
            }

            ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa");
            TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
            return View(model);
        }

        SignInWithSession(user, model.RememberMe);

        var role = user.Role;
        if (isDevelopmentPasswordlessLogin)
        {
            _logger.LogInformation(
                "Development passwordless login succeeded. Database role: {DatabaseRole}.",
                role);
        }

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
    public async Task<IActionResult> Register(AuthViewModel model, CancellationToken cancellationToken)
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
            Status = true
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            TempData["AlertSuccess"] = "Đăng ký thành công. Bạn có thể đăng nhập ngay.";
        }
        catch (DbUpdateException exception) when (IsDuplicateAccountError(exception))
        {
            AddDuplicateAccountError();
            return View(model);
        }

        return RedirectToAction(nameof(Login));
    }

    private void SignInWithSession(User user, bool rememberMe = false)
    {
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserFullName", user.FullName);
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetInt32("UserID", user.UserID);
        HttpContext.Session.SetString("RememberMe", rememberMe.ToString());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : null
        };

        HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties)
            .GetAwaiter()
            .GetResult();
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

    //Get: Lấy ra thông tin tài khoản gửi đến profile
    public IActionResult Profile()
    {
        var userID = HttpContext.Session.GetInt32("UserID");

        var user = _context.Users
        .Include(u => u.Bookings)
            .ThenInclude(b => b.Tickets)
                .ThenInclude(t => t.Showtime)
                    .ThenInclude(s => s!.Movie)
        .Include(u => u.Bookings)
            .ThenInclude(b => b.Tickets)
                .ThenInclude(t => t.Seat)
        .FirstOrDefault(u => u.UserID == userID.Value);

        return View(user);

    }
    //Post: Cập nhật tk
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Profile(User model)
    {

        ModelState.Remove(nameof(model.Role));
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var UserID = HttpContext.Session.GetInt32("UserID");
        if (UserID == null)
        {
            return RedirectToAction("Login");
        }
        //Lấy ra user
        var user = _context.Users.Find(UserID.Value);

        if (user == null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.DOB = model.DOB;
        user.PhoneNumber = model.PhoneNumber;

        _context.SaveChanges();

        TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
        return RedirectToAction(nameof(Profile));


    }
}
