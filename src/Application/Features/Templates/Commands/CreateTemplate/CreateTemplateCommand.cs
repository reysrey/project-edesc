using KaizenBoard.Application.Common.Security;
using KaizenBoard.Domain.Enums;
using MediatR;

namespace KaizenBoard.Application.Features.Templates.Commands.CreateTemplate;

[Authorize(Roles = "AdminL1,AdminL2")]
public class CreateTemplateCommand : IRequest<Guid>
{
    public TemplateType Type { get; init; }
    public Guid? DepartmentId { get; init; }
    public string ImprovementObject { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ProblemHint { get; init; } = string.Empty;
    public string SolutionHint { get; init; } = string.Empty;
}
