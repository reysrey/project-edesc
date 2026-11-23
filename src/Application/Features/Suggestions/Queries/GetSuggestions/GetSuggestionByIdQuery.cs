using MediatR;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public record GetSuggestionByIdQuery(Guid Id) : IRequest<SuggestionDetailsDto>;
