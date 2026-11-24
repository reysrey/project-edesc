using KaizenBoard.Infrastructure.Identity;

namespace KaizenBoard.Application.FunctionalTests;

public class SmokeTests : TestBase
{
    [Test]
    public async Task TestApp_can_create_user_and_read_it_back()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();

        userId.ShouldNotBeNullOrEmpty();
        TestApp.GetUserId().ShouldBe(userId);

        var user = await TestApp.FindAsync<ApplicationUser>(userId!);
        user.ShouldNotBeNull();
        user!.Email.ShouldBe("test@local");
    }
}
