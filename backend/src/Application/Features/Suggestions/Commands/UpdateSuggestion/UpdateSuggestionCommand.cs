using System.Text.Json.Serialization;
using KaizenBoard.Application.Common.Security;

namespace KaizenBoard.Application.Features.Suggestions.Commands.UpdateSuggestion;

[Authorize]
public class UpdateSuggestionCommand : IRequest<string>
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CurrentFlaws { get; set; } = string.Empty;
    public string ProposedSolution { get; set; } = string.Empty;
    public string ImprovementObject { get; set; } = string.Empty;
    public Guid TargetDepartmentId { get; set; }

    // Шаблон заявки; null — снять привязку.
    public Guid? TemplateId { get; set; }
    public bool? IsAnonymous { get; set; }

    // Версия из GET /{id}; при расхождении сервер отвечает 409.
    public string Version { get; set; } = string.Empty;
}
