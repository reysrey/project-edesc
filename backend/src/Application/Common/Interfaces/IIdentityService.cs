using KaizenBoard.Application.Common.Models;

namespace KaizenBoard.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    /// <summary>B01: человекочитаемое имя автора для карточки и списка.</summary>
    Task<string?> GetUserDisplayNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);
}
