using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Common.Models;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Enums;
using KaizenBoard.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public sealed class SearchSuggestionsQueryHandler
    : IRequestHandler<SearchSuggestionsQuery, PagedResult<SuggestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SearchSuggestionsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PagedResult<SuggestionDto>> Handle(
        SearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 100);

        var userId = _user.Id;
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException();

        var roles = _user.Roles ?? [];
        var isManager = roles.Contains(Roles.AdminL1);
        var isModerator = roles.Contains(Roles.AdminL2);

        // 1. Применяем видимость (B03.1) ДО всех фильтров
        var query = _context.Suggestions
            .AsNoTracking()
            .ApplyVisibility(userId, roles, _user.DepartmentId);

        // 2. Применяем scope (сужает, не расширяет)
        query = (request.Scope ?? "board") switch
        {
            "mine" => query.Where(s => s.CreatedBy == userId),
            "board" => query.Where(s => s.Status != SuggestionStatus.Draft),
            "moderation" when isModerator || isManager => query.Where(s =>
                s.Status == SuggestionStatus.Submitted ||
                s.Status == SuggestionStatus.InCommission ||
                s.Status == SuggestionStatus.ReturnedByManager ||
                s.Status == SuggestionStatus.RevisionRequired),
            "all" when isManager => query,
            _ => throw new ForbiddenAccessException(),
        };

        // 3. Текстовый поиск
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s =>
                EF.Functions.Like(s.Title, $"%{term}%") ||
                EF.Functions.Like(s.CurrentFlaws, $"%{term}%") ||
                EF.Functions.Like(s.ProposedSolution, $"%{term}%"));
        }

        // 4. Дополнительные фильтры
        if (request.Status is { } status)
            query = query.Where(s => s.Status == status);
        if (request.DepartmentId is { } departmentId)
            query = query.Where(s => s.TargetDepartmentId == departmentId);
        if (request.From is { } from)
            query = query.Where(s => s.Created >= from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (request.To is { } to)
            query = query.Where(s => s.Created < to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var totalCount = await query.CountAsync(cancellationToken);

        // 5. Сортировка
        query = request.Sort switch
        {
            "createdAsc" => query.OrderBy(s => s.Created),
            "title" => query.OrderBy(s => s.Title),
            "status" => query.OrderBy(s => s.Status).ThenByDescending(s => s.Created),
            _ => query.OrderByDescending(s => s.Created),
        };

        // 6. Подтягиваем подразделения для DTO
        var deptIds = query.Select(s => s.TargetDepartmentId).Distinct();
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(d => deptIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        // 7. Пагинация и проекция
        var rows = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

        var items = rows.Select(s => new SuggestionDto
        {
            Id = s.Id,
            Title = s.Title,
            Status = s.Status,
            Created = s.Created,
            CreatedBy = s.CreatedBy,
            AuthorName = s.CreatedBy, // TODO(B06): когда появится IsAnonymous
            TargetDepartmentId = s.TargetDepartmentId,
            DepartmentName = departments.GetValueOrDefault(s.TargetDepartmentId),
            ImprovementObject = s.ImprovementObject,
            ShortDescription = Shorten(s.CurrentFlaws, s.ProposedSolution),
            AllowedActions = SuggestionVisibility.ComputeActions(
                new Suggestion { Id = s.Id, Status = s.Status, CreatedBy = s.CreatedBy },
                userId, isManager, isModerator),
        }).ToList();

        return new PagedResult<SuggestionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    private static string Shorten(string? primary, string? fallback, int max = 200)
    {
        var text = !string.IsNullOrWhiteSpace(primary) ? primary : fallback ?? "";
        return text.Length <= max ? text : text[..max].TrimEnd() + "…";
    }
}
