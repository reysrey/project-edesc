using System;
using System.Collections.Generic;
using System.Text;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;

public class ReturnByManagerCommand: IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
}
