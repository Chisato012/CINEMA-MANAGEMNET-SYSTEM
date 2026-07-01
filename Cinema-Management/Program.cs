using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký MVC
builder.Services.AddControllersWithViews();

//Đăng ký session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Dùng để gọi API Cloudflare Turnstile
builder.Services.AddHttpClient();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var googleClientId =
    builder.Configuration["Authentication:Google:ClientId"];

var googleClientSecret =
    builder.Configuration["Authentication:Google:ClientSecret"];

// Trong moi truong Development, neu chua cau hinh Google OAuth
// thi khong dang ky Google authentication de ung dung van chay.
// Cookie authentication va che do dang nhap gia lap van hoat dong.
var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Production phai cau hinh ClientId va ClientSecret that
// truoc khi bat lai dang nhap Google.
if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = false;
        options.Events.OnCreatingTicket = context =>
        {
            if (context.User.TryGetProperty("email_verified", out var emailVerified))
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                identity?.AddClaim(new Claim(
                    "urn:google:email_verified",
                    emailVerified.ToString()));
            }

            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/Account/GoogleLoginFailed");
            return Task.CompletedTask;
        };
    });
}

// Lấy chuỗi kết nối từ appsettings.Development.json hoặc appsettings.json
var connectionString = builder.Configuration
                           .GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Không tìm thấy ConnectionStrings:DefaultConnection."
                       );

// Đăng ký ApplicationDbContext và cấu hình SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Kiểm tra kết nối database khi khởi động ứng dụng
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    try
    {
        var connected = await dbContext.Database.CanConnectAsync();

        Console.WriteLine(
            connected
                ? "Kết nối MovieTicketDB thành công!"
                : "Không thể kết nối MovieTicketDB!"
        );
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Lỗi kết nối database: {exception.Message}");
    }
}

// Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();


// Chi bat bo qua dang nhap trong moi truong Development va khi
// appsettings.Development.json dat DevelopmentLogin:Enabled = true.
// Tuyet doi khong them cau hinh nay vao Production.
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("DevelopmentLogin:Enabled"))
{
    app.Use(async (context, next) =>
    {
        // Chi tao tai khoan dang nhap gia lap khi request hien tai
        // chua co nguoi dung da dang nhap.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            var userId =
                app.Configuration.GetValue<int>(
                    "DevelopmentLogin:UserId");

            var email =
                app.Configuration[
                    "DevelopmentLogin:Email"];

            var fullName =
                app.Configuration[
                    "DevelopmentLogin:FullName"];

            var role =
                app.Configuration[
                    "DevelopmentLogin:Role"];

            // Tao cac claim tam thoi de he thong Authorize
            // nhan dien nguoi dung va vai tro trong luc phat trien.
            // Phan quyen theo vai tro van dung ClaimTypes.Role.
            // Doi Role trong appsettings.Development.json thanh Staff,
            // Admin hoac KhachHang de kiem thu cac quyen khac nhau.
            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),

                new(
                    ClaimTypes.Name,
                    fullName ?? "Development User"),

                new(
                    ClaimTypes.Email,
                    email ?? "staff@gmail.com"),

                new(
                    ClaimTypes.Role,
                    role ?? "Staff")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            var principal =
                new ClaimsPrincipal(identity);

            // Gan nguoi dung gia lap vao request hien tai.
            context.User = principal;

            // Luu cookie de cac request tiep theo van duoc xem
            // la da dang nhap trong moi truong Development.
            // Khi bam Logout, cookie co the bi xoa trong request do,
            // nhung request tiep theo se duoc dang nhap lai neu bypass con bat.
            await context.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal);
        }

        await next();
    });
}

// De khoi phuc luong dang nhap binh thuong trong Development,
// doi DevelopmentLogin:Enabled thanh false.
app.UseAuthorization();

// Định tuyến MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
