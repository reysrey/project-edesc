using KaizenBoard.Application.Common.Security;

namespace KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;

public class UpdateSuggestionCommand : IRequest<string>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CurrentFlaws { get; set; } = string.Empty;
    public string ProposedSolution { get; set; } = string.Empty;
    public string ImprovementObject { get; set; } = string.Empty;
    public Guid TargetDepartmentId { get; set; }

    // B02: шаблон заявки; null — снять привязку.
    public Guid? TemplateId { get; set; }

    // B02: версия из GET /{id}; при расхождении сервер отвечает 409.
    public string Version { get; set; } = string.Empty;
}
