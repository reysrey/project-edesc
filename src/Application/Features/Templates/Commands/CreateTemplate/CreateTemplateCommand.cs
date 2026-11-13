using MediatR;

namespace KaizenBoard.Application.Features.Templates.Commands.CreateTemplate;

public class CreateTemplateCommand : IRequest<Guid>
{
    public string Type { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string ImprovementObject { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ProblemHint { get; init; } = string.Empty;
    public string SolutionHint { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
