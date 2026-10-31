using System;
using System.Collections.Generic;
using System.Text;

namespace KaizenBoard.Domain.Entities;

public class Template
{
    public string Type { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string ImprovementObject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ProblemHint { get; set; } = string.Empty;
    public string SolutionHint { get; set; } = string.Empty;
    public bool IsActive { get; set; }

}
