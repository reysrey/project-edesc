using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Infrastructure.Data;
using KaizenBoard.Infrastructure.Data.Interceptors;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public const string DatabaseConnectionStringName = "KaizenBoardDb";

    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(DatabaseConnectionStringName);
        Guard.Against.Null(connectionString, message: $"Connection string '{DatabaseConnectionStringName}' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddAuthorizationBuilder();

        // AddIdentityApiEndpoints регистрирует сразу две схемы входа — cookie и Bearer —
        // и делает их общими по умолчанию. Без этого [Authorize] проверяет только cookie,
        // а заголовок Authorization: Bearer игнорируется.
        builder.Services
            .AddIdentityApiEndpoints<ApplicationUser>(_ => { })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Перекрывает дефолтный UserClaimsPrincipalFactory<ApplicationUser>,
        // зарегистрированный внутри AddIdentityApiEndpoints.
        // Добавляет claim department_id в cookie при логине (B03.8).
        builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
            ApplicationUserClaimsPrincipalFactory>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
    }
}
