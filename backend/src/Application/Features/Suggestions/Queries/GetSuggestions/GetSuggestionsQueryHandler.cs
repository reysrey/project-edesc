using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Entities;
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
        if (string.IsNullOrEmpty(_user.Id))
            throw new UnauthorizedAccessException();

        var roles = _user.Roles ?? [];
        var isManager = roles.Contains(Roles.AdminL1);
        var isModerator = roles.Contains(Roles.AdminL2);

        // Серверная видимость (B03.1) — до любых остальных фильтров.
        var rows = await _context.Suggestions
            .AsNoTracking()
            .ApplyVisibility(_user.Id, roles, _user.DepartmentId)
            .OrderByDescending(s => s.Created)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.Status,
                s.Created,
                s.CreatedBy,
                s.TargetDepartmentId,
                s.ImprovementObject,
                s.CurrentFlaws,
                s.ProposedSolution,
            })
            .ToListAsync(cancellationToken);

        // B01: подразделения и авторы — отдельными запросами, чтобы
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
            AllowedActions = SuggestionVisibility.ComputeActions(
                new Suggestion { Id = s.Id, Status = s.Status, CreatedBy = s.CreatedBy },
                _user.Id, isManager, isModerator),
        }).ToList();
    }

    private static string Shorten(string? primary, string? fallback, int max = 200)
    {
        var text = !string.IsNullOrWhiteSpace(primary) ? primary : fallback ?? "";
        return text.Length <= max ? text : text[..max].TrimEnd() + "…";
    }
}
