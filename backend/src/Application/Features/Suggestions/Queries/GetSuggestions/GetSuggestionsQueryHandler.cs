using KaizenBoard.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class GetSuggestionsQueryHandler : IRequestHandler<GetSuggestionsQuery, List<SuggestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public GetSuggestionsQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
    }

    public async Task<List<SuggestionDto>> Handle(GetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _user.Id;
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException();

        var roles = _user.Roles ?? [];
        var departmentId = _user.DepartmentId;
        var settings = await _context.ReadServiceSettingsAsync(cancellationToken);

        // Серверная видимость (B03.1) — до любых остальных фильтров.
        var rows = await _context.Suggestions
            .AsNoTracking()
            .ApplyVisibility(userId, roles, departmentId, settings.BoardEnabled)
            .OrderByDescending(s => s.Created)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.Status,
                s.Created,
                IsMine = s.CreatedBy == userId,
                CreatedBy = s.IsAnonymous && s.CreatedBy != userId ? null : s.CreatedBy,
                SupportCount = s.Supports.Count,
                SupportedByMe = s.Supports.Any(r => r.UserId == userId),
                s.TargetDepartmentId,
                s.ImprovementObject,
                CurrentFlaws = s.CurrentFlaws.Length > 201 ? s.CurrentFlaws.Substring(0, 201) : s.CurrentFlaws,
                ProposedSolution = s.ProposedSolution.Length > 201 ? s.ProposedSolution.Substring(0, 201) : s.ProposedSolution,
            })
            .ToListAsync(cancellationToken);

        // Подразделения и авторы — отдельными запросами, чтобы
        // не плодить N+1 на каждой строке списка.
        var departmentIds = rows.Select(s => s.TargetDepartmentId).Distinct().ToList();
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        var authorNames = new Dictionary<string, string?>();
        foreach (var authorId in rows.Select(s => s.CreatedBy).OfType<string>().Distinct())
        {
            authorNames[authorId] = await _identityService.GetUserDisplayNameAsync(authorId);
        }

        return rows.Select(s => new SuggestionDto
        {
            Id = s.Id,
            Title = s.Title,
            Status = s.Status,
            Created = s.Created,
            CreatedBy = s.CreatedBy,
            AuthorName = s.CreatedBy is null ? null : authorNames.GetValueOrDefault(s.CreatedBy),
            TargetDepartmentId = s.TargetDepartmentId,
            DepartmentName = departments.GetValueOrDefault(s.TargetDepartmentId),
            ImprovementObject = s.ImprovementObject,
            ShortDescription = Shorten(s.CurrentFlaws, s.ProposedSolution),
            SupportCount = s.SupportCount,
            SupportedByMe = s.SupportedByMe,
            AllowedActions = SuggestionVisibility.ComputeActions(
                s.Status, s.IsMine,
                SuggestionVisibility.CanModerate(roles, departmentId, s.TargetDepartmentId), settings.SupportEnabled),
        }).ToList();
    }

    private static string Shorten(string? primary, string? fallback, int max = 200)
    {
        var text = !string.IsNullOrWhiteSpace(primary) ? primary : fallback ?? "";
        return text.Length <= max ? text : text[..max].TrimEnd() + "…";
    }
}
