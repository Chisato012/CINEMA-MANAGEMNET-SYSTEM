using Cinema_Management.Data;
using Cinema_Management.Services.Chatbot;
using Cinema_Management.Services.Recommendation;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Dùng để gọi API Cloudflare Turnstile ở luồng đăng nhập/đăng ký.
builder.Services.AddHttpClient();

// Service ML.NET singleton chỉ cần load một lần.
// ChatbotService giới hạn lại, phụ thuộc ApplicationDbContext của từng request.
builder.Services.AddSingleton<IGenreRecommendationService, MlNetGenreRecommendationService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

var connectionString = builder.Configuration
                           .GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "Không tìm thấy ConnectionStrings:DefaultConnection."
                       );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Kiểm tra nhanh kết nối database khi app khởi động.
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
