using System;
using System.Collections.Generic;
using System.Text;

namespace KaizenBoard.Application.Features.Suggestions.Commands.ApproveByManagerCommand;

public class ApproveByManagerCommand: IRequest<Unit>
{
    public Guid Id { get; set; }
    public string RegNumber { get; set; } = string.Empty;
}
