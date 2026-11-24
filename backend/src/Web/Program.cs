using KaizenBoard.Infrastructure.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
await app.InitialiseDatabaseAsync();


app.UseHttpsRedirection();

// CORS для фронтенда: список источников задаётся в конфигурации (appsettings.json / переменные окружения)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
app.UseCors(policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    }
    else
    {
        // В разработке без настроенного списка — разрешаем всё, чтобы не мешать локальному запуску фронта
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    }
});

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/scalar"));

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);
app.Run();
