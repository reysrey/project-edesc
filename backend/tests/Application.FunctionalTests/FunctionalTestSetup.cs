using KaizenBoard.Application.FunctionalTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace KaizenBoard.Application.FunctionalTests;

[SetUpFixture]
public class FunctionalTestSetup
{
    internal static IServiceScopeFactory ScopeFactory { get; private set; } = null!;
    internal static DatabaseResetter? DbResetter { get; private set; }

    private static WebApiFactory? _factory;
    private static PostgreSqlContainer? _container;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var cancellationToken = cts.Token;

        // Поднимаем реальный PostgreSQL в Docker через Testcontainers
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .Build();

        await _container.StartAsync(cancellationToken);

        var connectionString = _container.GetConnectionString();

        _factory = new WebApiFactory(connectionString);
        ScopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        DbResetter = await DatabaseResetter.CreateAsync(connectionString);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (DbResetter is not null) await DbResetter.DisposeAsync();
        if (_factory is not null) await _factory.DisposeAsync();
        if (_container is not null) await _container.DisposeAsync();
    }
}
