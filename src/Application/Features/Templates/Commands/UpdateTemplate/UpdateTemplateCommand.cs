using KaizenBoard.Application.Common.Security;
using KaizenBoard.Domain.Constants;
using KaizenBoard.Domain.Enums;
using MediatR;

namespace KaizenBoard.Application.Features.Templates.Commands.UpdateTemplate;

[Authorize(Roles = Roles.AdminL1)]
public class UpdateTemplateCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
    public TemplateType Type { get; init; }
    public Guid? DepartmentId { get; init; }
    public string ImprovementObject { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ProblemHint { get; init; } = string.Empty;
    public string SolutionHint { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
