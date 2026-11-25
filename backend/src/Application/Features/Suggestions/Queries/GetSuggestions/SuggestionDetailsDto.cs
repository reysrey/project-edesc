using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class SuggestionDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Draft;
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public string? AuthorName { get; set; }
    public bool IsMine { get; set; }
    public Guid TargetDepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string ImprovementObject { get; set; } = string.Empty;
    public string CurrentFlaws { get; set; } = string.Empty;
    public string ProposedSolution { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? CommissionNote { get; set; }
    public List<string> AllowedActions { get; set; } = [];

    // B01: непрозрачный маркер конкурентного изменения.
    // Клиент возвращает его в PUT, при устаревании сервер отвечает 409.
    public string Version { get; set; } = string.Empty;
}
