using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Security;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ReturnByManager;

[Authorize(Roles = "AdminL1,AdminL2")]
public class ReturnByManagerCommand: IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
}
