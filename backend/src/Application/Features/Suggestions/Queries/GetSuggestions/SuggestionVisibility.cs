using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public static class SuggestionVisibility
{
    /// <summary>
    /// Сужает набор до записей, видимых пользователю.
    /// Применять ДО любых фильтров scope/search/status/department.
    /// </summary>
    public static IQueryable<Suggestion> ApplyVisibility(
        this IQueryable<Suggestion> source,
        string userId,
        IReadOnlyCollection<string>? roles,
        Guid? userDepartmentId)
    {
        var isManager = roles?.Contains(Roles.AdminL1) == true;
        var isModerator = roles?.Contains(Roles.AdminL2) == true;

        // Черновик виден только автору, включая пользователей с повышенными правами.
        if (isManager)
            return source.Where(s => s.CreatedBy == userId || s.Status != SuggestionStatus.Draft);

        if (isModerator)
        {
            return source.Where(s =>
                s.CreatedBy == userId ||
                (s.Status != SuggestionStatus.Draft &&
                    userDepartmentId != null && s.TargetDepartmentId == userDepartmentId));
        }

        return source.Where(s =>
            s.CreatedBy == userId ||
            (s.Status != SuggestionStatus.Draft && s.Status != SuggestionStatus.Rejected));
    }

    public static List<string> ComputeActions(
        Suggestion s, string userId, bool canModerate)
    {
        var actions = new List<string>();
        var isMine = s.CreatedBy == userId;

        if (isMine && s.Status is SuggestionStatus.Draft
            or SuggestionStatus.ReturnedByManager
            or SuggestionStatus.RevisionRequired)
        {
            actions.Add("edit");
            actions.Add("submit");
        }

        if (canModerate)
        {
            if (s.Status == SuggestionStatus.Submitted) actions.Add("moderate");
            if (s.Status == SuggestionStatus.InCommission)
            {
                actions.Add("commissionApprove");
                actions.Add("commissionReturn");
                actions.Add("commissionReject");
            }
            if (s.Status == SuggestionStatus.ApprovedPlanning) actions.Add("implementationStart");
            if (s.Status == SuggestionStatus.InImplementation) actions.Add("implementationComplete");
        }

        return actions;
    }
}
