using KaizenBoard.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace KaizenBoard.Domain.UnitTests.Constants;

public class RolesTests
{
    [Test]
    public void RoleNames_AreStable()
    {
        // Роли используются в сиде БД и в атрибутах [Authorize] — имена менять нельзя без миграции данных.
        Roles.User.ShouldBe("User");
        Roles.AdminL1.ShouldBe("AdminL1");
        Roles.AdminL2.ShouldBe("AdminL2");
    }
}
