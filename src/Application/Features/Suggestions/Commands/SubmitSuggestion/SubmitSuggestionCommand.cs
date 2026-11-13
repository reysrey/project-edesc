using System;
using System.Collections.Generic;
using System.Text;

namespace KaizenBoard.Application.Features.Suggestions.Commands.SubmitSuggestion;

public class SubmitSuggestionCommand: IRequest<Unit>
{
    public Guid Id { get; set; }
}
