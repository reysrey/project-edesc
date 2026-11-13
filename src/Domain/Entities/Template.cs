using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Domain.Entities;

public class Template : BaseAuditableEntity
{
    public TemplateType Type { get; set; }
    public Guid? DepartmentId { get; set; }
    public string ImprovementObject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ProblemHint { get; set; } = string.Empty;
    public string SolutionHint { get; set; } = string.Empty;
    public bool IsActive { get; set; }

}
