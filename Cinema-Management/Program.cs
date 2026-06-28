using Cinema_Management.Data;
using Cinema_Management.Models;
using Cinema_Management.Services;
using System.Security.Claims;
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

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "Cinema.GoogleOAuth";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = false;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
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

app.UseAuthorization();

// Định tuyến MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
