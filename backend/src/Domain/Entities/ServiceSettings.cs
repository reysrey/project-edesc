namespace KaizenBoard.Domain.Entities;

public class ServiceSettings
{
    public int Id { get; init; } = 1;
    public bool SupportEnabled { get; set; } = true;
    public bool AnonymousEnabled { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public bool BoardEnabled { get; set; } = true;
    public Guid Version { get; set; }
}
