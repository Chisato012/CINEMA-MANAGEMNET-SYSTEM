using Cinema_Management.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Lấy chuỗi kết nối từ appsettings.Development.json hoặc appsettings.json
var connectionString = builder.Configuration
                           .GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Không tìm thấy ConnectionStrings:DefaultConnection."
                       );

// Đăng ký ApplicationDbContext và cấu hình SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var jwKey = builder.Configuration["Jwt:Key"]; //Lấy key từ appsettings.Development.json hoặc appsettings.json
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            //Kiểm tra server phát hành đúng token hay không
            ValidateIssuer = true,

            //Kierm tra client nhận token có đúng không
            ValidateAudience = true,
            //Kiểm tra thời gian sống của token
            ValidateLifetime = true,
            //Kiểm tra key mã hóa token
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwKey)),
            ClockSkew = TimeSpan.Zero //Loại bỏ thời gian trễ khi kiểm tra thời gian sống của token
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Bình thường JWT Bearer đọc token từ header Authorization.
                // Nhưng khi truy cập UI /Staff/Index bằng browser, browser không tự gắn header đó.
                // Vì vậy ta đọc JWT từ cookie "access_token".
                var token = context.Request.Cookies["access_token"];

                if (!string.IsNullOrWhiteSpace(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                // Nếu truy cập UI mà chưa đăng nhập, chuyển về trang Login thay vì hiện 401 trắng.
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    context.HandleResponse();
                    context.Response.Redirect("/Account/Login");
                }

                return Task.CompletedTask;
            }
        };
    });
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