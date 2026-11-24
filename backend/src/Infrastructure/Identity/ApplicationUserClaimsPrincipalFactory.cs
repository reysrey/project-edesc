using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KaizenBoard.Infrastructure.Identity;

public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> options)
        : base(userManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.DepartmentId is { } depId)
            identity.AddClaim(new Claim("department_id", depId.ToString()));
        return identity;
    }
}
