using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class SuggestionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Draft;
    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }
    public string? AuthorName { get; set; }
    public Guid TargetDepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string ImprovementObject { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int SupportCount { get; set; }
    public bool SupportedByMe { get; set; }
    public List<string> AllowedActions { get; set; } = [];
}
