using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Domain.Entities;

public class SuggestionActivity : BaseEntity
{
    public Guid SuggestionId { get; init; }
    public string ActorId { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
    public SuggestionStatus FromStatus { get; init; }
    public SuggestionStatus ToStatus { get; init; }
    public string? Note { get; init; }
    public bool NotifyAuthor { get; init; }
}
