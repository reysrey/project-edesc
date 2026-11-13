using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Interfaces;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class GetSuggestionsQueryHandler : IRequestHandler<GetSuggestionsQuery, List<SuggestionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSuggestionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<List<SuggestionDto>> Handle(GetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var list = await _context.Suggestions
            .AsNoTracking()
    .Select(s => new SuggestionDto { Id = s.Id, Title = s.Title, Status = s.Status, Created = s.Created })
    .ToListAsync(cancellationToken);
        return list;
    }

}
