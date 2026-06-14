namespace Cinema_Management.Controllers;

using Cinema_Management.Data;
using Cinema_Management.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class AccountController : Controller
{
    // Đọc cấu hình từ appsettings.json
    // Ví dụ: Cloudflare Turnstile SiteKey và SecretKey
    private readonly IConfiguration _configuration;
    
    // Dùng để tạo HttpClient gửi request tới Cloudflare
    private readonly IHttpClientFactory _httpClientFactory;
    
    // DbContext dùng để truy vấn và lưu dữ liệu trong database
    private readonly ApplicationDbContext _context;
    
    
    // Dependency Injection:
    // ASP.NET Core tự động truyền các service cần thiết vào Controller
    public AccountController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }
    
    // =====================================================
    // HIỂN THỊ TRANG ĐĂNG NHẬP
    // GET: /Account/Login
    // =====================================================
    [HttpGet]
    public IActionResult Login()
    {
        // Lấy SiteKey của Cloudflare Turnstile
        // và truyền sang View thông qua ViewBag
        SetTurnstileSiteKey();
        return View();
    }

    // =====================================================
    // XỬ LÝ FORM ĐĂNG NHẬP MVC
    // POST: /Account/Login
    // =====================================================
    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        // Khi trả lại View do có lỗi, View vẫn cần SiteKey
        // để hiển thị lại CAPTCHA        
        SetTurnstileSiteKey();
        
        // Lấy token CAPTCHA do Cloudflare Turnstile gửi từ form
        model.CaptchaToken = Request.Form["cf-turnstile-response"].ToString();
        
        // CaptchaToken không được gửi bằng asp-for,
        // nên loại thuộc tính này khỏi ModelState
        // và kiểm tra CAPTCHA riêng ở phía dưới
        ModelState.Remove(nameof(LoginRequest.CaptchaToken));

        
        // Kiểm tra các validation của Email và Password
        // trong LoginRequest
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        // Gửi token CAPTCHA tới Cloudflare để xác thực
        if (!await IsTurnstileValidAsync())
        {
            ViewBag.CaptchaError = "Vui long xac minh captcha.";
            return View(model);
        }

        // Chuẩn hóa email trước khi tìm kiếm
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == model.Email);

        
        // Nếu không tìm thấy tài khoản hoặc mật khẩu không đúng
        // thì trả về cùng một thông báo để tránh lộ email tồn tại
        if (user == null || !IsPasswordValid(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Sai email hoặc mật khẩu");
            return View(model);
        }

        // Kiểm tra trạng thái tài khoản
        // Status = false nghĩa là tài khoản đã bị khóa
        if (!user.Status)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa");
            return View(model);
        }

        
        // Đăng nhập thành công thì chuyển sang trang Home
        // Hiện tại chưa tạo Cookie, Session hoặc JWT
        return RedirectToAction("Index", "Home");
    }

    
     // =====================================================
        // API ĐĂNG NHẬP
        // POST: /login
        //
        // Request body:
        // {
        //     "email": "example@gmail.com",
        //     "password": "Test@123"
        // }
        // =====================================================
    [HttpPost("login")]
    public async Task<IActionResult> LoginApi([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
        {
            return Unauthorized("Sai email hoặc mật khẩu");
        }

        if (!user.Status)
        {
            return Unauthorized("Tài khoản đã bị khóa");
        }

        if (!IsPasswordValid(request.Password, user.PasswordHash))
        {
            return Unauthorized("Sai email hoặc mật khẩu");
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

    // =====================================================
    // HIỂN THỊ TRANG ĐĂNG KÝ
    // GET: /Account/Register
    // =====================================================
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AuthViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Chuẩn hóa email
        var email = model.Email.Trim().ToLower();

        // Kiểm tra email đã tồn tại
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng");
            return View(model);
        }

        // Chuyển từ ViewModel sang User Entity
        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            PhoneNumber = model.PhoneNumber.Trim(),
            DOB = model.DateOfBirth,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Status = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký thành công. Hãy đăng nhập.";

        return RedirectToAction(nameof(Login));
    }

    // Logic xu ly luu database tao tai khoan

    private void SetTurnstileSiteKey()
    {
        ViewBag.TurnstileSiteKey = _configuration["CloudflareTurnstile:SiteKey"];
    }

    private async Task<bool> IsTurnstileValidAsync()
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
                }));

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>();
            return result?.Success == true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private static bool IsPasswordValid(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
