using KaizenBoard.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using KaizenBoard.Application.Common.Models;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Enums;
using KaizenBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapIdentityApi<ApplicationUser>();

        groupBuilder.MapPost(Logout, "logout").RequireAuthorization();
        groupBuilder.MapGet(GetCurrentUser, "me").RequireAuthorization();
        groupBuilder.MapGet(GetEmployees, "directory").RequireAuthorization(policy => policy.RequireRole(Roles.AdminL1));
        groupBuilder.MapGet(GetNotifications, "notifications").RequireAuthorization();
    }

    public static async Task<Ok<PagedResult<EmployeeResponse>>> GetEmployees(
        ApplicationDbContext db, CancellationToken cancellationToken, string? search, string? role,
        Guid? departmentId, int pageNumber = 1, int pageSize = 20)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var employeeRoles = from assignment in db.UserRoles
            join identityRole in db.Roles on assignment.RoleId equals identityRole.Id
            select new { assignment.UserId, identityRole.Name };
        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            term = term[..Math.Min(term.Length, 200)];
            var pattern = $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            users = users.Where(u => EF.Functions.ILike(u.FullName, pattern) ||
                EF.Functions.ILike(u.Email!, pattern) || EF.Functions.ILike(u.Position, pattern));
        }
        if (departmentId.HasValue)
            users = users.Where(u => u.DepartmentId == departmentId);
        if (!string.IsNullOrWhiteSpace(role))
            users = users.Where(u => employeeRoles.Any(r => r.UserId == u.Id && r.Name == role));

        var count = await users.CountAsync(cancellationToken);
        var offset = (long)(pageNumber - 1) * pageSize;
        List<EmployeeResponse> items = offset >= count ? [] : await users
            .OrderBy(u => u.FullName).ThenBy(u => u.Email).ThenBy(u => u.Id)
            .Skip((int)offset).Take(pageSize)
            .Select(u => new EmployeeResponse(
                u.Id, u.Email ?? string.Empty, u.FullName, u.Position, u.DepartmentId,
                db.Departments.Where(d => d.Id == u.DepartmentId).Select(d => d.Name).FirstOrDefault(),
                employeeRoles.Where(r => r.UserId == u.Id).Select(r => r.Name!).OrderBy(r => r).ToArray()))
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(new PagedResult<EmployeeResponse>
        {
            Items = items, TotalCount = count, PageNumber = pageNumber, PageSize = pageSize
        });
    }

    public static async Task<Ok<List<NotificationResponse>>> GetNotifications(
        ClaimsPrincipal principal, UserManager<ApplicationUser> userManager, ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(principal) ?? throw new UnauthorizedAccessException();
        var items = await (
            from activity in db.SuggestionActivities.AsNoTracking()
            join suggestion in db.Suggestions on activity.SuggestionId equals suggestion.Id
            where activity.NotifyAuthor && suggestion.CreatedBy == userId
            orderby activity.OccurredAt descending, activity.Id
            select new NotificationResponse(activity.Id, suggestion.Id, suggestion.Title, activity.ToStatus, activity.OccurredAt)
        ).Take(20).ToListAsync(cancellationToken);
        return TypedResults.Ok(items);
    }

    public record EmployeeResponse(
        string Id, string Email, string FullName, string Position, Guid? DepartmentId, string? DepartmentName, string[] Roles);

    public record NotificationResponse(
        Guid Id, Guid SuggestionId, string Title, SuggestionStatus Status, DateTimeOffset OccurredAt);

    public static async Task<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>> GetCurrentUser(
    System.Security.Claims.ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return TypedResults.Unauthorized();

        var roles = await userManager.GetRolesAsync(user);

        // B03.5: Если у пользователя нет ролей (только зарегистрировался), выдаем базовую роль User
        if (roles.Count == 0)
        {
            await userManager.AddToRoleAsync(user, KaizenBoard.Domain.Constants.Roles.User);
            roles = await userManager.GetRolesAsync(user);
        }

        return TypedResults.Ok(new CurrentUserResponse(
            user.Id, user.Email ?? string.Empty, user.FullName, user.Position,
            user.DepartmentId, roles.ToArray()));
    }

    public sealed record CurrentUserResponse(
        string Id, string Email, string FullName, string Position,
        Guid? DepartmentId, string[] Roles);

    [EndpointSummary("Log out")]
    [EndpointDescription("Logs out the current user by clearing the authentication cookie.")]
    public static async Task<Results<Ok, UnauthorizedHttpResult>> Logout(SignInManager<ApplicationUser> signInManager, [FromBody] object empty)
    {
        if (empty != null)
        {
            await signInManager.SignOutAsync();
            return TypedResults.Ok();
        }

        return TypedResults.Unauthorized();
    }
}
