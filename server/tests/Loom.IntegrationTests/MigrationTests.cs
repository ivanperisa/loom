using Loom.DevSeed;
using Loom.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.IntegrationTests;

public class MigrationTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Model_has_no_changes_without_a_migration()
    {
        var pending = await Db(db => Task.FromResult(db.Database.HasPendingModelChanges()));
        Assert.False(pending, "The EF model changed but no migration was added (dotnet ef migrations add <Name>).");
    }

    [Fact]
    public async Task Demo_seed_runs_end_to_end_on_a_fresh_database()
    {
        var connectionString = _fixture.ConnectionStringFor("seed_" + Guid.NewGuid().ToString("N")[..8]);
        await using var services = DatabaseFixture.BuildServices(connectionString);

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(Ct);
            await ReferenceData.LoadAsync(db);
        }

        // Every scenario goes through the real services; any failing step throws.
        await new DemoData(services, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).SeedAsync();

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scenarioGuids = Enumerable.Range(1, 14).Select(DemoData.ExchangeGuid).ToList();
            Assert.Equal(14, await db.Exchanges.CountAsync(e => scenarioGuids.Contains(e.Guid), Ct));
            Assert.True(await db.Recognitions.AnyAsync(r => r.Status == Domain.Enums.DocumentStatus.Approved, Ct));
        }
    }
}
