using KaizenBoard.Application.Common.Exceptions;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Application.Common.Models;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public sealed class SearchSuggestionsQueryHandler
    : IRequestHandler<SearchSuggestionsQuery, PagedResult<SuggestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public SearchSuggestionsQueryHandler(IApplicationDbContext context, IUser user, IIdentityService identityService)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
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
        var userDepartmentId = _user.DepartmentId;
        var settings = await _context.ReadServiceSettingsAsync(cancellationToken);

        // 1. Применяем видимость (B03.1) ДО всех фильтров
        var query = _context.Suggestions
            .AsNoTracking()
            .ApplyVisibility(userId, roles, userDepartmentId, settings.BoardEnabled);

        // 2. Применяем scope (сужает, не расширяет)
        query = (request.Scope ?? "board") switch
        {
            "mine" => query.Where(s => s.CreatedBy == userId),
            "board" => query.Where(s => s.Status != SuggestionStatus.Draft),
            "moderation" when isModerator || isManager => query.Where(s =>
                (isManager || s.TargetDepartmentId == userDepartmentId) &&
                (s.Status == SuggestionStatus.Submitted ||
                    s.Status == SuggestionStatus.InCommission ||
                    s.Status == SuggestionStatus.ReturnedByManager ||
                    s.Status == SuggestionStatus.RevisionRequired)),
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
        if (request.To is { } to && to < DateOnly.MaxValue)
            query = query.Where(s => s.Created < to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (long)(pageNumber - 1) * pageSize;
        if (offset >= totalCount)
        {
            return new PagedResult<SuggestionDto>
            {
                Items = [],
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };
        }

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
            .Skip((int)offset)
            .Take(pageSize)
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

        var isMineScope = request.Scope == "mine";
        var authorName = isMineScope ? await _identityService.GetUserDisplayNameAsync(userId) : null;

        var items = rows.Select(s => new SuggestionDto
        {
            Id = s.Id,
            Title = s.Title,
            Status = s.Status,
            Created = s.Created,
            CreatedBy = s.CreatedBy,
            AuthorName = isMineScope ? authorName : s.CreatedBy,
            TargetDepartmentId = s.TargetDepartmentId,
            DepartmentName = departments.GetValueOrDefault(s.TargetDepartmentId),
            ImprovementObject = s.ImprovementObject,
            ShortDescription = Shorten(s.CurrentFlaws, s.ProposedSolution),
            SupportCount = s.SupportCount,
            SupportedByMe = s.SupportedByMe,
            AllowedActions = SuggestionVisibility.ComputeActions(
                s.Status, s.IsMine,
                SuggestionVisibility.CanModerate(roles, userDepartmentId, s.TargetDepartmentId), settings.SupportEnabled),
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
