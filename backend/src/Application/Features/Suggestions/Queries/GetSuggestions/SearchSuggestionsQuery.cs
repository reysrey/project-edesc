using KaizenBoard.Application.Common.Models;
using KaizenBoard.Domain.Enums;
using MediatR;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public sealed record SearchSuggestionsQuery : IRequest<PagedResult<SuggestionDto>>
{
    public string? Scope { get; init; }              // mine|board|moderation|all
    public string? Search { get; init; }
    public SuggestionStatus? Status { get; init; }
    public Guid? DepartmentId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? Sort { get; init; }               // createdDesc|createdAsc|title|status
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
