using KaizenBoard.Domain.Enums; // <-- Добавили
using MediatR;

namespace KaizenBoard.Application.Features.Templates.Commands.UpdateTemplate;

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
