using KaizenBoard.Domain.Entities;
using NUnit.Framework;

namespace KaizenBoard.Domain.UnitTests.Entities;

[TestFixture]
public class TemplateTests
{
    [Test]
    public void Archive_Should_Set_IsActive_To_False()
    {
        var template = new Template { IsActive = true };
        template.Archive();
        Assert.That(template.IsActive, Is.False, "Флаг IsActive должен был стать false после архивации.");
    }
}
