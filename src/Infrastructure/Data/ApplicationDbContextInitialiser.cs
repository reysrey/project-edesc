using KaizenBoard.Domain.Constants;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KaizenBoard.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        var roles = new[] { Roles.AdminL1, Roles.AdminL2, Roles.User };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Не удалось создать роль {Role}: {Errors}", role,
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminEmail = "admin@localhost";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Главный Администратор",
                Position = "Админ"
            };

            var createResult = await _userManager.CreateAsync(adminUser, "Admin123!");

            if (!createResult.Succeeded)
            {
                _logger.LogError("Не удалось создать администратора: {Errors}",
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        // Роль назначаем всегда, даже если пользователь уже существовал:
        // иначе пользователь, созданный раньше, останется без прав.
        if (!await _userManager.IsInRoleAsync(adminUser, Roles.AdminL1))
        {
            var roleResult = await _userManager.AddToRoleAsync(adminUser, Roles.AdminL1);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Не удалось назначить роль {Role}: {Errors}", Roles.AdminL1,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
