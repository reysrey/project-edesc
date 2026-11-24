using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Application.Features.Templates.Queries.GetTemplates;

public class TemplateDto
{
    public Guid Id { get; set; }
    public TemplateType Type { get; set; }
    public Guid? DepartmentId { get; set; }
    public string ImprovementObject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ProblemHint { get; set; } = string.Empty;
    public string SolutionHint { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
