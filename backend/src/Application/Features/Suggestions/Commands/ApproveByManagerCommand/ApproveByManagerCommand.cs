using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Security;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ApproveByManagerCommand;

[Authorize(Roles = "AdminL1,AdminL2")]
public class ApproveByManagerCommand: IRequest<Unit>
{
    public Guid Id { get; set; }
    public string RegNumber { get; set; } = string.Empty;
}
