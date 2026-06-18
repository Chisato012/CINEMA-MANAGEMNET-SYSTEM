using Customer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient<LmStudioClient>();
builder.Services.AddSingleton<MovieDataService>();
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddSingleton<ChatOrchestrator>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CinemaWeb", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("CinemaWeb");

app.UseAuthorization();

app.MapControllers();

app.Run();
