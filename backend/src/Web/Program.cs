using KaizenBoard.Infrastructure.Data;
using KaizenBoard.Web.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256 * 1024);
// Add services to the container.
builder.AddServiceDefaults();

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
await app.InitialiseDatabaseAsync();

// Accept the original HTTPS scheme from the local reverse proxy only.
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto });
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.UseExceptionHandler(options => { });

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    app.UseCors(policy => policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!BrowserOriginProtection.Allows(context.Request, allowedOrigins))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }
    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.Map("/", () => Results.Redirect("/scalar"));
}

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);
app.Run();
