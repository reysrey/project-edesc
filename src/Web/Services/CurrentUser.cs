using System.Security.Claims;
using KaizenBoard.Application.Common.Interfaces;

namespace KaizenBoard.Web.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? Id =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public List<string>? Roles =>
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
            .Select(x => x.Value).ToList();

    public Guid? DepartmentId =>
        Guid.TryParse(
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("department_id"),
            out var id) ? id : null;
}
