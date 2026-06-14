using Cinema_Management.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký MVC
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

// Định tuyến MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();