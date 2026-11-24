using KaizenBoard.Application.Common.Security;

namespace KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;

public class UpdateSuggestionCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CurrentFlaws { get; set; } = string.Empty;
    public string ProposedSolution { get; set; } = string.Empty;
    public string ImprovementObject { get; set; } = string.Empty;
    public Guid TargetDepartmentId { get; set; }
}
