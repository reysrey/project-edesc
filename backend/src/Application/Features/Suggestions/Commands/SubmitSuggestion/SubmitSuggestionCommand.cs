using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Security;

namespace KaizenBoard.Application.Features.Suggestions.Commands.SubmitSuggestion;

[Authorize]
public class SubmitSuggestionCommand: IRequest<Unit>
{
    public Guid Id { get; set; }
}
