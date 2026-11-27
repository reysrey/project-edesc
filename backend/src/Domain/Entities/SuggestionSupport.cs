namespace KaizenBoard.Domain.Entities;

public class SuggestionSupport
{
    public Guid SuggestionId { get; init; }
    public string UserId { get; init; } = string.Empty;
}
