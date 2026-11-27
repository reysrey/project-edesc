using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Application.Features.Suggestions;

public static class SuggestionVisibility
{
    public static IQueryable<Suggestion> ExcludeForeignDrafts(this IQueryable<Suggestion> source, string? userId) =>
        source.Where(s => s.Status != SuggestionStatus.Draft || s.CreatedBy == userId);

    /// <summary>
    /// Сужает набор до записей, видимых пользователю.
    /// Применять ДО любых фильтров scope/search/status/department.
    /// </summary>
    public static IQueryable<Suggestion> ApplyVisibility(
        this IQueryable<Suggestion> source,
        string userId,
        IReadOnlyCollection<string>? roles,
        Guid? userDepartmentId,
        bool boardEnabled)
    {
        var isManager = roles?.Contains(Roles.AdminL1) == true;
        var isModerator = roles?.Contains(Roles.AdminL2) == true;

        // Черновик виден только автору, включая пользователей с повышенными правами.
        source = source.ExcludeForeignDrafts(userId);
        if (isManager)
            return source;

        if (isModerator)
        {
            return source.Where(s =>
                s.CreatedBy == userId ||
                (userDepartmentId != null && s.TargetDepartmentId == userDepartmentId));
        }

        if (!boardEnabled)
            return source.Where(s => s.CreatedBy == userId);

        return source.Where(s =>
            s.CreatedBy == userId || s.Status != SuggestionStatus.Rejected);
    }

    public static bool CanModerate(IReadOnlyCollection<string>? roles, Guid? userDepartmentId, Guid targetDepartmentId) =>
        roles?.Contains(Roles.AdminL1) == true ||
        (roles?.Contains(Roles.AdminL2) == true && userDepartmentId == targetDepartmentId);

    public static bool CanSupport(SuggestionStatus status, bool isMine, bool supportEnabled) =>
        supportEnabled && !isMine && status is not SuggestionStatus.Draft and not SuggestionStatus.Rejected;

    public static List<string> ComputeActions(
        SuggestionStatus status, bool isMine, bool canModerate, bool supportEnabled)
    {
        var actions = new List<string>();
        if (isMine && Suggestion.CanEdit(status))
            actions.Add("edit");
        if (isMine && Suggestion.CanSubmit(status))
            actions.Add("submit");
        if (canModerate && Suggestion.CanBeModerated(status))
            actions.Add("moderate");
        if (canModerate && status == SuggestionStatus.InCommission)
            actions.Add("commission");
        if (CanSupport(status, isMine, supportEnabled))
            actions.Add("support");

        return actions;
    }
}
