using System.Text.Json.Serialization.Metadata;
using TestModel.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoRecommendationService>();
builder.Services.AddScoped<MovieQueryService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.TypeInfoResolverChain.Clear();
        options.JsonSerializerOptions.TypeInfoResolverChain.Add(
            new DefaultJsonTypeInfoResolver()
        );
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ILmStudioService, LmStudioService>(client =>
{
    var baseUrl = builder.Configuration["LmStudio:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new Exception("Chưa cấu hình LmStudio:BaseUrl trong appsettings.json");
    }

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowWeb");

app.UseAuthorization();

app.MapControllers();

app.Run();