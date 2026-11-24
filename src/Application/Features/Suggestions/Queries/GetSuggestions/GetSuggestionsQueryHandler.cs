using KaizenBoard.Application.Common.Interfaces;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class GetSuggestionsQueryHandler : IRequestHandler<GetSuggestionsQuery, List<SuggestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetSuggestionsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<SuggestionDto>> Handle(
        GetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
            throw new UnauthorizedAccessException();

        return await _context.Suggestions
            .AsNoTracking()
            .ApplyVisibility(_user.Id, _user.Roles, _user.DepartmentId)
            .OrderByDescending(s => s.Created)
            .Select(s => new SuggestionDto
            {
                Id = s.Id,
                Title = s.Title,
                Status = s.Status,
                Created = s.Created,
            })
            .ToListAsync(cancellationToken);
    }
}
