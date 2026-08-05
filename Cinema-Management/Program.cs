using Cinema_Management.Data;
using Cinema_Management.Services.Chatbot;
using Cinema_Management.Services.Recommendation;
using Microsoft.EntityFrameworkCore;
using Cinema_Management.Models;
using Cinema_Management.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Lệnh train chạy riêng, không khởi động web server.
// Chạy tại folder CINEMA-MANAGEMNET-SYSTEM:
// dotnet run --project Cinema-Management\Cinema-Management.csproj -- --train-recommendation-model
// sẽ gọi sang MovieGenreModelTrainer.Train() để train model từ CSV và lưu ra file zip.
if (args.Contains("--train-recommendation-model", StringComparer.OrdinalIgnoreCase))
{
    var dataPath = MovieGenreModelTrainer.GetDefaultDataPath(builder.Environment.ContentRootPath);
    var modelPath = MlNetGenreRecommendationService.GetDefaultModelPath(builder.Environment.ContentRootPath);
    var result = MovieGenreModelTrainer.Train(dataPath, modelPath);

    Console.WriteLine($"Recommendation model trained: {result.ModelPath}");
    Console.WriteLine($"Rows: {result.RowCount}; Labels: {result.LabelCount}");
    Console.WriteLine($"MicroAccuracy: {result.MicroAccuracy:0.###}; MacroAccuracy: {result.MacroAccuracy:0.###}; LogLoss: {result.LogLoss:0.###}");
    return;
}

var viCulture = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = viCulture;
CultureInfo.DefaultThreadCurrentUICulture = viCulture;

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services
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

// Service ML.NET singleton chỉ cần load một lần.
// ChatbotService giới hạn lại, phụ thuộc ApplicationDbContext của từng request.
builder.Services.AddSingleton<IGenreRecommendationService, MlNetGenreRecommendationService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

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
