using Loom.Application;
using Loom.DevSeed;
using Loom.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Usage: dotnet run --project server/Loom.DevSeed [-- --reset]
//   (no args)  apply migrations, then seed reference + demo data if the database is empty
//   --reset    drop the database first (everything is recreated)
//   --migrate-only  apply migrations, no data
var reset = args.Contains("--reset");
var migrateOnly = args.Contains("--migrate-only");

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=loom;Username=loom;Password=loom";

var services = new ServiceCollection()
    .AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information)
        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
        // MigrateAsync probes the database before creating it; that expected failure is logged as an error.
        .AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.None))
    .AddInfrastructure(connectionString)
    .AddApplication()
    .BuildServiceProvider();

var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevSeed");

await using (var scope = services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (reset)
    {
        log.LogWarning("Dropping database");
        await db.Database.EnsureDeletedAsync();
    }

    log.LogInformation("Applying migrations");
    await db.Database.MigrateAsync();

    if (migrateOnly) return 0;

    if (await db.Users.AnyAsync())
    {
        log.LogInformation("Database already has data, skipping seed (use --reset to start over)");
        return 0;
    }

    log.LogInformation("Loading reference data");
    await ReferenceData.LoadAsync(db);
}

log.LogInformation("Seeding demo data");
await new DemoData(services, log).SeedAsync();
log.LogInformation("Done");
return 0;
