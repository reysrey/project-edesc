using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Security;
using MediatR;

namespace KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;

[Authorize]
public class CreateSuggestionCommand: IRequest<Guid>
{
    public string Title { get; init; } = string.Empty;
    public string CurrentFlaws { get; init; } = string.Empty;
    public string ProposedSolution { get; init; } = string.Empty;
    public string ImprovementObject { get; init; } = string.Empty;
    public Guid TargetDepartmentId { get; init; }
}
