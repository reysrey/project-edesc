using KaizenBoard.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace KaizenBoard.Domain.UnitTests.Constants;

public class RolesTests
{
    [Test]
    public void AdministratorRoleName_IsStable()
    {
        // Роль используется в сиде БД и в тестах — имя менять нельзя без миграции данных
        Roles.Administrator.ShouldBe("Administrator");
    }
}
