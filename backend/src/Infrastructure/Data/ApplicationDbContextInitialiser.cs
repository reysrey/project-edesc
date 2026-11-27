using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KaizenBoard.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync(app.Environment.IsDevelopment());
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

    public async Task SeedAsync(bool seedDevelopmentData)
    {
        try
        {
            await TrySeedAsync(seedDevelopmentData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync(bool seedDevelopmentData)
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

        if (!seedDevelopmentData)
            return;

        // --- Главный администратор (AdminL1) ---
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
            var roleResult = await _userManager.AddToRoleAsync(adminUser, Roles.AdminL1);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Не удалось назначить роль {Role}: {Errors}", Roles.AdminL1,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        // --- Тестовое подразделение (IT) ---
        var defaultDept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == "IT");
        if (defaultDept == null)
        {
            defaultDept = new Department
            {
                Name = "IT",
                EmployeesCount = 10
            };
            _context.Departments.Add(defaultDept);
            await _context.SaveChangesAsync();
        }

        // --- Тестовый модератор (AdminL2) ---
        var modEmail = "moderator@localhost";
        var modUser = await _userManager.FindByEmailAsync(modEmail);

        if (modUser == null)
        {
            modUser = new ApplicationUser
            {
                UserName = modEmail,
                Email = modEmail,
                FullName = "Тестовый Модератор",
                Position = "Менеджер",
                DepartmentId = defaultDept.Id
            };

            var createModResult = await _userManager.CreateAsync(modUser, "Moderator123!");
            if (!createModResult.Succeeded)
            {
                _logger.LogError("Не удалось создать модератора: {Errors}",
                    string.Join("; ", createModResult.Errors.Select(e => e.Description)));
                return;
            }

            var roleResult = await _userManager.AddToRoleAsync(modUser, Roles.AdminL2);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Не удалось назначить роль {Role}: {Errors}", Roles.AdminL2,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        var employeeEmail = "employee@localhost";
        if (await _userManager.FindByEmailAsync(employeeEmail) is null)
        {
            var employee = new ApplicationUser
            {
                UserName = employeeEmail,
                Email = employeeEmail,
                FullName = "Сотрудник IdeaHub",
                Position = "Сотрудник",
                DepartmentId = defaultDept.Id
            };
            var result = await _userManager.CreateAsync(employee, "Mvp01Demo!2026");
            if (result.Succeeded)
                result = await _userManager.AddToRoleAsync(employee, Roles.User);
            if (!result.Succeeded)
                _logger.LogError("Не удалось подготовить сотрудника: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        foreach (var name in new[] { "Производство", "Контроль качества" })
        {
            if (!await _context.Departments.AnyAsync(d => d.Name == name))
                _context.Departments.Add(new Department { Name = name });
        }
        await _context.SaveChangesAsync();
    }
}
