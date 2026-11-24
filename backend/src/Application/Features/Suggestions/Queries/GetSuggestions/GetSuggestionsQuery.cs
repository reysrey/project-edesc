using System;
using System.Collections.Generic;
using System.Text;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public record class GetSuggestionsQuery: IRequest<List<SuggestionDto>>
{
}
