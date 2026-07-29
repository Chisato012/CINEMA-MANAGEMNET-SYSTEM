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
// BE Bắc
public class AccountController : Controller
{
    private const string GoogleProvider = "Google";
    private const string GoogleRememberMeSessionKey = "Google_RememberMe";
    private const string RememberMeAuthItemKey = "rememberMe";
    private const string CaptchaFailureMessage = "Xác minh CAPTCHA thất bại. Vui lòng thử lại.";
    private const string TurnstileConfigurationFailureMessage = "Cloudflare Turnstile chưa được cấu hình đầy đủ.";
    private static readonly TimeSpan TurnstileVerifyTimeout = TimeSpan.FromSeconds(5);
    private static readonly HashSet<string> DevelopmentPasswordlessEmails =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "khach@gmail.com",
            "staff@gmail.com",
            "admin@gmail.com"
        };
    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ResendConfirmationCooldown = TimeSpan.FromMinutes(2);

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        SetTurnstileViewData();
        ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, CancellationToken cancellationToken)
    {
        SetTurnstileViewData();

        model.CaptchaToken = Request.Form["cf-turnstile-response"].ToString();
        ModelState.Remove(nameof(LoginRequest.CaptchaToken));

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

        if (!IsTurnstileEnabled())
        {
            LogTurnstileConfigurationError();
            ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
            ViewBag.CaptchaError = TurnstileConfigurationFailureMessage;
            ModelState.AddModelError(string.Empty, TurnstileConfigurationFailureMessage);
            TempData["AlertError"] = TurnstileConfigurationFailureMessage;
            return View(model);
        }

        if (!await IsTurnstileValidAsync(model.CaptchaToken, cancellationToken))
        {
            ViewBag.ShowDevelopmentPasswordlessLoginMessage = _environment.IsDevelopment();
            ViewBag.CaptchaError = CaptchaFailureMessage;
            ModelState.AddModelError(string.Empty, CaptchaFailureMessage);
            TempData["AlertError"] = CaptchaFailureMessage;
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

        if (!user.EmailConfirmed)
        {
            TempData["AlertError"] = "Vui lòng xác nhận email trước khi đăng nhập.";
            return RedirectToAction(nameof(RegisterPending), new { email = user.Email });
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
            Status = true,
            EmailConfirmed = false
        };

        _context.Users.Add(user);

        try
        {
            var confirmationEmailSent = await CreateAndSendConfirmationEmailAsync(user, cancellationToken);
            TempData[confirmationEmailSent ? "AlertSuccess" : "AlertError"] = confirmationEmailSent
                ? "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản."
                : "Đăng ký thành công nhưng chưa gửi được email xác nhận. Vui lòng gửi lại email xác nhận.";
        }
        catch (DbUpdateException exception) when (IsDuplicateAccountError(exception))
        {
            AddDuplicateAccountError();
            return View(model);
        }

        return RedirectToAction(nameof(RegisterPending), new { email });
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
            return ConfirmEmailFailedView(
                normalizedEmail,
                "Liên kết xác nhận không hợp lệ hoặc đã được sử dụng.");
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
    
    [HttpGet]
public IActionResult GoogleRegister()
{
    var email = HttpContext.Session.GetString("Google_Email");
    var fullName = HttpContext.Session.GetString("Google_FullName");

    if (string.IsNullOrEmpty(email))
    {
        return RedirectToAction(nameof(Login));
    }

    // Đổ dữ liệu có sẵn từ Google ra View thông qua AuthViewModel
    var model = new AuthViewModel
    {
        Email = email,
        FullName = fullName
    };

    return View(model);
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleRegister(AuthViewModel model, CancellationToken cancellationToken)
    {
        var googleId = HttpContext.Session.GetString("Google_Id");
        var email = HttpContext.Session.GetString("Google_Email");

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        {
            return RedirectToAction(nameof(Login));
        }

        // 🔥 1. Xóa bỏ kiểm tra Password và Username (nếu không dùng) để ModelState.IsValid không bị fail vô lý
        ModelState.Remove(nameof(AuthViewModel.Password));
        ModelState.Remove(nameof(AuthViewModel.ConfirmPassword));
        ModelState.Remove(nameof(AuthViewModel.Username));

        // 2. Nếu dữ liệu nhập thêm (Họ tên, SĐT, Ngày sinh) bị sai định dạng Regex
        if (!ModelState.IsValid)
        {
            // Trả lại View kèm dữ liệu đã nhập để hiển thị lỗi đỏ thông báo cho khách hàng
            return View(model);
        }

        // 3. Nếu dữ liệu hợp lệ hoàn toàn, tiến hành lưu vào DB
        var normalizedEmail = NormalizeEmail(email);
        var rememberMe = HttpContext.Session.GetString(GoogleRememberMeSessionKey) == bool.TrueString;
        var user = await FindUserByGoogleIdAsync(googleId, cancellationToken);

        if (user == null)
        {
            var existingEmailUser = await FindUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

            if (existingEmailUser != null)
            {
                ClearGoogleRegistrationSession();
                TempData["AlertError"] = GoogleLoginFlow.IsLocalAccount(existingEmailUser)
                    ? GoogleLoginFlow.ExistingLocalAccountMessage
                    : GoogleLoginFlow.DifferentExternalProviderMessage;
                return RedirectToAction(nameof(Login));
            }
            else
            {
                user = new User
                {
                    FullName = model.FullName.Trim(),
                    Email = normalizedEmail,
                    PhoneNumber = model.PhoneNumber?.Trim(),
                    DOB = model.DateOfBirth,
                    PasswordHash = null,
                    Role = "KhachHang",
                    Status = true,
                    EmailConfirmed = false,
                    ExternalProvider = GoogleProvider,
                    ExternalProviderKey = googleId
                };

                _context.Users.Add(user);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException exception) when (IsDuplicateAccountError(exception))
                {
                    _context.Entry(user).State = EntityState.Detached;
                    var duplicateEmailUser = await FindUserByNormalizedEmailAsync(
                        normalizedEmail,
                        cancellationToken);

                    ClearGoogleRegistrationSession();
                    TempData["AlertError"] = duplicateEmailUser != null
                        ? GoogleLoginFlow.IsLocalAccount(duplicateEmailUser)
                            ? GoogleLoginFlow.ExistingLocalAccountMessage
                            : GoogleLoginFlow.DifferentExternalProviderMessage
                        : "Không thể tạo tài khoản Google này.";
                    return RedirectToAction(nameof(Login));
                }
            }
        }

        if (!user.Status)
        {
            TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
            return RedirectToAction(nameof(Login));
        }

        ClearGoogleRegistrationSession();

        if (!user.EmailConfirmed)
        {
            var confirmationEmailSent = await CreateAndSendConfirmationEmailAsync(user, cancellationToken);
            TempData[confirmationEmailSent ? "AlertSuccess" : "AlertError"] = confirmationEmailSent
                ? "Đăng ký Google thành công. Vui lòng kiểm tra Gmail để xác nhận tài khoản."
                : "Đăng ký Google thành công nhưng chưa gửi được email xác nhận. Vui lòng gửi lại email xác nhận.";

            return RedirectToAction(nameof(RegisterPending), new { email = user.Email });
        }

        SignInWithSession(user, rememberMe);

        TempData["AlertSuccess"] = $"Đăng nhập Google thành công! Xin chào {user.FullName}";
        return RedirectByRole(GetUserRole(user));
    }

// Hàm phụ trợ thực hiện gửi email chào mừng
private async Task<bool> SendWelcomeEmailAsync(User user, CancellationToken cancellationToken)
{
    var safeFullName = System.Net.WebUtility.HtmlEncode(user.FullName);
    var htmlBody = $"""
        <div style="background:#121212; color:#f5f5f5; padding:32px; font-family:Arial,sans-serif; text-align:center;">
            <h1 style="color:#D4A373; margin-bottom:16px;">COSMOS CINEMA</h1>
            <h2 style="color:#ffffff;">Chào mừng {safeFullName}!</h2>
            <p style="color:#aaa; font-size:16px; line-height:1.6;">
                Tài khoản của bạn đã được khởi tạo thành công thông qua kết nối Google. <br/>
                Từ bây giờ, bạn đã có thể trải nghiệm dịch vụ đặt vé phim trực tuyến cực nhanh tại hệ thống của chúng tôi.
            </p>
            <div style="margin-top:32px;">
                <a href="{Url.Action("Index", "Home", null, Request.Scheme)}" style="background:#A67B5B; color:#ffffff; padding:12px 24px; text-decoration:none; border-radius:6px; font-weight:bold;">
                    Trải Nghiệm Ngay
                </a>
            </div>
        </div>
        """;

    return await _emailService.SendEmailAsync(
        user.Email,
        "Chào mừng bạn đến với COSMOS Cinema!",
        htmlBody,
        cancellationToken
    );
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

        if (user == null || user.EmailConfirmed)
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

        var confirmationEmailSent = await SendConfirmationEmailAsync(user, verificationToken, cancellationToken);
        if (!confirmationEmailSent)
        {
            user.EmailVerificationLastSentAt = null;
            await _context.SaveChangesAsync(cancellationToken);
        }

        TempData[confirmationEmailSent ? "AlertSuccess" : "AlertError"] = confirmationEmailSent
            ? "Neu email hop le va chua xac nhan, chung toi se gui lien ket xac nhan moi."
            : "Chua gui duoc email xac nhan. Vui long kiem tra cau hinh SMTP.";

        return RedirectToAction(nameof(RegisterPending), new { email = normalizedEmail });
    }

    [HttpGet]
    public IActionResult GoogleLogin(bool rememberMe = false)
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
            RedirectUri = redirectUrl,
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddMinutes(10)
        };
        properties.Items[RememberMeAuthItemKey] = rememberMe.ToString();

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
    var rememberMe = IsRememberMeRequested(authenticateResult.Properties);
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

    // 1. Kiểm tra xem tài khoản liên kết Google này đã tồn tại trong DB chưa
    var user = await FindUserByGoogleIdAsync(googleId, cancellationToken);
    var existingEmailUser = user == null
        ? await FindUserByNormalizedEmailAsync(email, cancellationToken)
        : null;

    var decision = GoogleLoginFlow.Decide(
        googleId,
        email,
        emailVerified,
        user,
        existingEmailUser);

    switch (decision.Type)
    {
        case GoogleLoginDecisionType.InvalidGoogleProfile:
            TempData["AlertError"] = "Google không trả về đủ thông tin tài khoản.";
            return RedirectToAction(nameof(Login));

        case GoogleLoginDecisionType.UnverifiedGoogleEmail:
            TempData["AlertError"] = "Google chưa xác minh email của tài khoản này.";
            return RedirectToAction(nameof(Login));

        case GoogleLoginDecisionType.InactiveAccount:
            TempData["AlertError"] = "Tài khoản của bạn đã bị khóa.";
            return RedirectToAction(nameof(Login));

        case GoogleLoginDecisionType.ExistingUnconfirmedGoogleAccount:
            return RedirectToAction(nameof(RegisterPending), new { email = decision.User!.Email });

        case GoogleLoginDecisionType.ExistingLocalAccount:
            ClearGoogleRegistrationSession();
            TempData["AlertError"] = GoogleLoginFlow.ExistingLocalAccountMessage;
            return RedirectToAction(nameof(Login));

        case GoogleLoginDecisionType.DifferentExternalProvider:
            ClearGoogleRegistrationSession();
            TempData["AlertError"] = GoogleLoginFlow.DifferentExternalProviderMessage;
            return RedirectToAction(nameof(Login));

        case GoogleLoginDecisionType.NewGoogleRegistrationRequired:
            HttpContext.Session.SetString("Google_Email", email);
            HttpContext.Session.SetString("Google_FullName", fullName.Trim());
            HttpContext.Session.SetString("Google_Id", googleId);
            HttpContext.Session.SetString(GoogleRememberMeSessionKey, rememberMe.ToString());
            return RedirectToAction(nameof(GoogleRegister));

        case GoogleLoginDecisionType.ExistingGoogleAccount:
            user = decision.User!;
            break;

        default:
            TempData["AlertError"] = "Đăng nhập Google thất bại hoặc đã bị hủy.";
            return RedirectToAction(nameof(Login));
    }

    SignInWithSession(user, rememberMe);

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

    private async Task<bool> SendRegistrationWelcomeEmailAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var safeFullName = WebUtility.HtmlEncode(user.FullName);
        var homeLink = Url.Action("Index", "Home", null, Request.Scheme);
        var safeHomeLink = WebUtility.HtmlEncode(homeLink ?? "/");
        var htmlBody = $"""
            <!doctype html>
            <html>
            <body style="margin:0;background:#121212;font-family:Arial,sans-serif;color:#f5f5f5;">
                <div style="max-width:560px;margin:0 auto;padding:32px 24px;text-align:center;">
                    <h1 style="color:#D4A373;margin:0 0 12px;">COSMOS Cinema</h1>
                    <h2 style="color:#ffffff;margin:0 0 16px;">Welcome, {safeFullName}!</h2>
                    <p style="color:#ddd;font-size:16px;line-height:1.6;">
                        Your COSMOS Cinema account has been activated successfully.
                    </p>
                    <p style="margin:28px 0;">
                        <a href="{safeHomeLink}" style="background:#A67B5B;color:#ffffff;text-decoration:none;padding:14px 22px;border-radius:8px;font-weight:bold;display:inline-block;">
                            Start booking tickets
                        </a>
                    </p>
                </div>
            </body>
            </html>
            """;

        return await _emailService.SendEmailAsync(
            user.Email,
            "Welcome to COSMOS Cinema!",
            htmlBody,
            cancellationToken);
    }

    private async Task<bool> CreateAndSendConfirmationEmailAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var verificationToken = CreateEmailVerificationToken(user);
        await _context.SaveChangesAsync(cancellationToken);

        var confirmationEmailSent = await SendConfirmationEmailAsync(
            user,
            verificationToken,
            cancellationToken);

        if (!confirmationEmailSent)
        {
            user.EmailVerificationLastSentAt = null;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return confirmationEmailSent;
    }

    private async Task<bool> SendConfirmationEmailAsync(
        User user,
        string verificationToken,
        CancellationToken cancellationToken)
    {
        var confirmationLink = BuildEmailConfirmationLink(user.Email, verificationToken);

        if (string.IsNullOrWhiteSpace(confirmationLink))
        {
            _logger.LogWarning("Could not create email confirmation URL for user {UserID}.", user.UserID);
            return false;
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
                    <p style="color:#ddd;font-size:14px;line-height:1.6;">
                        Nếu nút không hoạt động, hãy sao chép liên kết này và mở trong trình duyệt:
                    </p>
                    <p style="word-break:break-all;color:#D4A373;font-size:13px;line-height:1.6;">
                        {safeLink}
                    </p>
                    <p>Liên kết này có hiệu lực trong 30 phút.</p>
                    <p style="color:#aaa;font-size:13px;">Nếu bạn không tạo tài khoản, vui lòng bỏ qua email này.</p>
                </div>
            </body>
            </html>
            """;

        return await _emailService.SendEmailAsync(
            user.Email,
            "Xác nhận tài khoản COSMOS Cinema",
            htmlBody,
            cancellationToken);
    }

    private string? BuildEmailConfirmationLink(string email, string verificationToken)
    {
        var publicBaseUrl = _configuration["Application:PublicBaseUrl"]?.Trim().TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(publicBaseUrl)
            && Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri))
        {
            var confirmEmailUrl = new Uri(baseUri, "/Account/ConfirmEmail").ToString();
            return QueryHelpers.AddQueryString(
                confirmEmailUrl,
                new Dictionary<string, string?>
                {
                    ["email"] = email,
                    ["token"] = verificationToken
                });
        }

        return Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { email, token = verificationToken },
            Request.Scheme);
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

    private static bool IsRememberMeRequested(AuthenticationProperties? properties)
    {
        return properties?.Items.TryGetValue(RememberMeAuthItemKey, out var value) == true
               && bool.TryParse(value, out var rememberMe)
               && rememberMe;
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

    private Task<User?> FindUserByGoogleIdAsync(
        string googleId,
        CancellationToken cancellationToken)
    {
        return _context.Users
            .FirstOrDefaultAsync(
                u => u.ExternalProvider == GoogleProvider
                     && u.ExternalProviderKey == googleId,
                cancellationToken);
    }

    private void ClearGoogleRegistrationSession()
    {
        HttpContext.Session.Remove("Google_Email");
        HttpContext.Session.Remove("Google_FullName");
        HttpContext.Session.Remove("Google_Id");
        HttpContext.Session.Remove(GoogleRememberMeSessionKey);
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

    private bool IsTurnstileEnabled()
    {
        return !string.IsNullOrWhiteSpace(_configuration["CloudflareTurnstile:SiteKey"])
               && !string.IsNullOrWhiteSpace(_configuration["CloudflareTurnstile:SecretKey"]);
    }

    private void LogTurnstileConfigurationError()
    {
        _logger.LogError(
            "Cloudflare Turnstile is not fully configured. SiteKey configured: {SiteKeyConfigured}; SecretKey configured: {SecretKeyConfigured}.",
            !string.IsNullOrWhiteSpace(_configuration["CloudflareTurnstile:SiteKey"]),
            !string.IsNullOrWhiteSpace(_configuration["CloudflareTurnstile:SecretKey"]));
    }

    private void SetTurnstileViewData()
    {
        var isTurnstileEnabled = IsTurnstileEnabled();
        ViewBag.IsTurnstileEnabled = isTurnstileEnabled;
        ViewBag.TurnstileSiteKey = isTurnstileEnabled
            ? _configuration["CloudflareTurnstile:SiteKey"]
            : null;
    }

    private async Task<bool> IsTurnstileValidAsync(string token, CancellationToken cancellationToken)
    {
        var secretKey = _configuration["CloudflareTurnstile:SecretKey"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secretKey))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var timeoutCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellationTokenSource.CancelAfter(TurnstileVerifyTimeout);

            var response = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token,
                    ["remoteip"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                }),
                timeoutCancellationTokenSource.Token);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(
                timeoutCancellationTokenSource.Token);
            return result?.Success == true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogWarning(exception, "Cloudflare Turnstile request timed out.");
            return false;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Cloudflare Turnstile request failed.");
            return false;
        }
        catch (NotSupportedException exception)
        {
            _logger.LogWarning(exception, "Cloudflare Turnstile response could not be parsed.");
            return false;
        }
        catch (System.Text.Json.JsonException exception)
        {
            _logger.LogWarning(exception, "Cloudflare Turnstile response was invalid.");
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
        return GoogleLoginFlow.IsGoogleEmailVerified(principal);
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
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
        if(!ModelState.IsValid)
        {
            return View(model);
        }
        var UserID = HttpContext.Session.GetInt32("UserID");
        if(UserID == null)
        {
            return RedirectToAction("Login");
        }
        //Lấy ra user
        var user = _context.Users.Find(UserID.Value);

        if(user == null)
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
