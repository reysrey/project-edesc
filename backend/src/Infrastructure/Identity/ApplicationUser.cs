using Microsoft.AspNetCore.Identity;

namespace KaizenBoard.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public Guid? DepartmentId { get; set; }
}
