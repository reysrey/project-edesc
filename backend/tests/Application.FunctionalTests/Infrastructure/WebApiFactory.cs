using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KaizenBoard.Application.FunctionalTests.Infrastructure;

public class WebApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Production")
            .UseSetting("ConnectionStrings:KaizenBoardDb", connectionString);

        builder.ConfigureTestServices(services =>
        {
            services
                .RemoveAll<IUser>()
                .AddTransient<IUser>(provider =>
                {
                    var accessor = provider.GetRequiredService<IHttpContextAccessor>();
                    if (accessor.HttpContext is not null)
                        return new CurrentUser(accessor);

                    var mock = new Mock<IUser>();
                    mock.SetupGet(x => x.Roles).Returns(TestApp.GetRoles());
                    mock.SetupGet(x => x.Id).Returns(TestApp.GetUserId());
                    mock.SetupGet(x => x.DepartmentId).Returns(TestApp.GetDepartmentId()); // ← новое
                    return mock.Object;
                });
        });
    }
}
