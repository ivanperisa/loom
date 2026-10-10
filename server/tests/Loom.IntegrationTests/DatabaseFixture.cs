using Loom.Application;
using Loom.DevSeed;
using Loom.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Loom.IntegrationTests;

/// <summary>
/// One PostgreSQL container per test run, migrated with the real EF migrations and loaded with the
/// reference catalogue. Tests create their own users/exchanges with unique emails, so they don't interfere.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    // Override with LOOM_TEST_POSTGRES_IMAGE to match the production major version.
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder(Environment.GetEnvironmentVariable("LOOM_TEST_POSTGRES_IMAGE") ?? "postgres:17-alpine").Build();

    public IServiceProvider Services { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        Services = BuildServices(ConnectionString);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await ReferenceData.LoadAsync(db);
    }

    public static ServiceProvider BuildServices(string connectionString) =>
        new ServiceCollection().AddLogging().AddInfrastructure(connectionString).AddApplication().BuildServiceProvider();

    /// <summary>Connection string for a separate, empty database on the same server.</summary>
    public string ConnectionStringFor(string database) =>
        new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString) { Database = database }.ConnectionString;

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
