namespace Cinema_Management.Controllers;

using Cinema_Management.Data;
using Cinema_Management.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;

    public AccountController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        SetTurnstileSiteKey();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        SetTurnstileSiteKey();
        model.CaptchaToken = Request.Form["cf-turnstile-response"].ToString();
        ModelState.Remove(nameof(LoginRequest.CaptchaToken));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await IsTurnstileValidAsync())
        {
            ViewBag.CaptchaError = "Vui long xac minh captcha.";
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == model.Email);

        if (user == null || !IsPasswordValid(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Sai email hoặc mật khẩu");
            return View(model);
        }

        if (!user.Status)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa");
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }

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

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(AuthViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return RedirectToAction("Login");
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
