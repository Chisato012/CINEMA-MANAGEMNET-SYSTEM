using System.Text.RegularExpressions;
using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.Services.Authentication;
using Cinema_Management.Services.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cinema_Management.Controllers;
 
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
    private readonly IConfiguration _configuration;
    private readonly EmailVerificationService _emailVerificationService;
    private readonly UserSignInService _userSignInService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IWebHostEnvironment environment,
        ApplicationDbContext context,
        IConfiguration configuration,
        EmailVerificationService emailVerificationService,
        UserSignInService userSignInService,
        ILogger<AccountController> logger)
    {
        _environment = environment;
        _context = context;
        _configuration = configuration;
        _emailVerificationService = emailVerificationService;
        _userSignInService = userSignInService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        PopulateAuthViewData();
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
            PopulateAuthViewData();
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

            PopulateAuthViewData();
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

            PopulateAuthViewData();
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa");
            TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
            return View(model);
        }

        if (!user.EmailConfirmed)
        {
            PopulateAuthViewData(user.Email);
            ModelState.AddModelError(
                string.Empty,
                "Tài khoản chưa xác minh email. Vui lòng kiểm tra hộp thư hoặc gửi lại email xác minh.");
            TempData["AlertError"] = "Vui lòng xác minh email trước khi đăng nhập.";
            return View(model);
        }

        await _userSignInService.SignInAsync(
            HttpContext,
            user,
            model.RememberMe);

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

        if (!user.EmailConfirmed)
        {
            return Unauthorized("Tai khoan chua xac minh email");
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
        PopulateAuthViewData();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AuthViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            PopulateAuthViewData();
            return View(model);
        }

        var email = NormalizeEmail(model.Email);
        var emailExists = await UserEmailExistsAsync(email, cancellationToken);

        if (emailExists)
        {
            AddDuplicateAccountError();
            PopulateAuthViewData();
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

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateAccountError(exception))
        {
            AddDuplicateAccountError();
            PopulateAuthViewData();
            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to create account for email {Email}.",
                email);
            TempData["AlertError"] = "Chưa tạo được tài khoản, vui lòng thử lại.";
            PopulateAuthViewData();
            return View(model);
        }

        var token = _emailVerificationService.GenerateAndApplyToken(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await SendVerificationEmailAsync(user, token, cancellationToken);
            TempData["AlertSuccess"] = "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.";
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send verification email for user {UserId}.",
                user.UserID);
            TempData["AlertError"] = "Tài khoản đã được tạo nhưng chưa gửi được email xác minh. Vui lòng thử gửi lại.";
        }

        return RedirectToAction(nameof(EmailVerificationSent), new { email = user.Email });
    }

    [HttpGet]
    public IActionResult EmailVerificationSent(string email)
    {
        ViewBag.Email = email;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(
        int userId,
        string token,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        if (user == null || !_emailVerificationService.IsTokenValid(user, token))
        {
            TempData["AlertError"] = "Lien ket xac minh email khong hop le hoac da het han.";
            return RedirectToAction(nameof(Login));
        }

        _emailVerificationService.MarkEmailConfirmed(user);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["AlertSuccess"] = "Xac minh email thanh cong. Ban co the dang nhap.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerificationEmail(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            TempData["AlertError"] = "Vui long nhap email de gui lai xac minh.";
            return RedirectToAction(nameof(Login));
        }

        var user = await FindUserByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user == null)
        {
            TempData["AlertSuccess"] = "Nếu email tồn tại, hệ thống sẽ gửi lại email xác minh.";
            return RedirectToAction(nameof(Login));
        }

        if (user.EmailConfirmed)
        {
            TempData["AlertSuccess"] = "Email này đã được xác minh. Bạn có thể đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        if (!user.Status)
        {
            TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
            return RedirectToAction(nameof(Login));
        }

        if (!_emailVerificationService.CanSendVerificationEmail(user))
        {
            TempData["AlertError"] = "Vui lòng đợi một chút trước khi gửi lại email xác minh.";
            return RedirectToAction(nameof(EmailVerificationSent), new { email = user.Email });
        }

        var token = _emailVerificationService.GenerateAndApplyToken(user);

        try
        {
            await SendVerificationEmailAsync(user, token, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            TempData["AlertSuccess"] = "Đã gửi lại email xác minh. Vui lòng kiểm tra hộp thư.";
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to resend verification email for user {UserId}.",
                user.UserID);
            TempData["AlertError"] = "Chua gui duoc email xac minh. Vui long thu lai sau.";
        }

        return RedirectToAction(nameof(EmailVerificationSent), new { email = user.Email });
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

    private async Task SendVerificationEmailAsync(
        User user,
        string token,
        CancellationToken cancellationToken)
    {
        var verificationLink = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new
            {
                userId = user.UserID,
                token
            },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(verificationLink))
        {
            throw new InvalidOperationException(
                "Could not generate email verification link.");
        }

        await _emailVerificationService.SendVerificationEmailAsync(
            user,
            verificationLink,
            cancellationToken);
    }

    private void PopulateAuthViewData(string? unconfirmedEmail = null)
    {
        ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
        ViewBag.GoogleLoginEnabled = IsGoogleLoginEnabled();
        ViewBag.UnconfirmedEmail = unconfirmedEmail;
    }

    private bool IsGoogleLoginEnabled()
    {
        return !string.IsNullOrWhiteSpace(
                   _configuration["Authentication:Google:ClientId"])
               && !string.IsNullOrWhiteSpace(
                   _configuration["Authentication:Google:ClientSecret"]);
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
     
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Profile(User model)
    {

        ModelState.Remove(nameof(model.Role));
        var UserID = HttpContext.Session.GetInt32("UserID");
        if (UserID == null)
        {
            return RedirectToAction("Login");
        }
         
        var user = _context.Users.Find(UserID.Value);

        if (user == null)
        {
            return NotFound();
        }

        if (model.DOB > DateTime.Now)
        {
            ModelState.AddModelError(nameof(model.DOB), "Ngày sinh không hợp lệ.");
        }
        if (model.FullName == null || model.FullName.Trim().Length > 50)
        {
            ModelState.AddModelError(nameof(model.FullName), "Họ và tên không được vượt quá 50 ký tự và để trống");
        }

        if (model.PhoneNumber == null)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại k được để trống");

        }

        var fullName = model.FullName?.Trim() ?? string.Empty;
        var phoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;

        string namePattern = @"^[\p{L}\s]+$";
        if (Regex.IsMatch(fullName, namePattern) == false)
        {
            ModelState.AddModelError(nameof(model.FullName), "Họ và tên chỉ được chứa chữ cái và khoảng trắng.");
        }

        const string phonePattern = @"^0[0-9]{9}$";
        if (!Regex.IsMatch(phoneNumber, phonePattern))
        {
            ModelState.AddModelError(
                nameof(model.PhoneNumber),
                "Số điện thoại phải có đúng 10 chữ số và bắt đầu bằng số 0.");
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message));

            TempData["AlertError"] = string.Join(" ", errors);

            return View(model);
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
