using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class SuggestionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Draft;
    public DateTimeOffset Created { get; set; }
}
