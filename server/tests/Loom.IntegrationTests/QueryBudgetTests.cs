using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Loom.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.IntegrationTests;

/// <summary>
/// How many SQL commands a request runs. The count must not grow with the data (no query per row, "N+1"),
/// and stays under a small budget so a new lazy query shows up in review.
/// </summary>
public class QueryBudgetTests(DatabaseFixture fixture) : HttpTest(fixture)
{
    private sealed class SqlCounter : DbCommandInterceptor
    {
        private int _count;
        public int Count => _count;
        public void Reset() => Interlocked.Exchange(ref _count, 0);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private (WebApplicationFactory<Program> Factory, SqlCounter Counter) Counted()
    {
        var counter = new SqlCounter();
        var factory = Factory().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
            services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(counter))));
        return (factory, counter);
    }

    /// <summary>SQL commands one GET runs (after a warm-up call, so caches are as in normal use).</summary>
    private static async Task<int> Queries(HttpClient client, SqlCounter counter, string url)
    {
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url, Ct)).StatusCode);
        counter.Reset();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url, Ct)).StatusCode);
        return counter.Count;
    }

    private Task<string> Email(int userId) => Db(db => db.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync(Ct));

    [Fact]
    public async Task The_coordinator_student_list_runs_the_same_queries_for_one_student_or_many()
    {
        var (factory, counter) = Counted();
        await using var _ = factory;
        var (partner, _) = await NewPartner();
        var coordinator = await NewUser(UserRole.Coordinator);
        var client = await LoggedIn(factory, await Email(coordinator));
        const string url = "/api/coordinator/students?page=1&pageSize=25";

        await NewExchange(await NewUser(), partner, coordinator);
        var one = await Queries(client, counter, url);

        for (var i = 0; i < 9; i++)
        {
            var student = await NewUser();
            await NewExchange(student, partner, coordinator);
            await NewExchange(student, partner, coordinator);
        }
        var many = await Queries(client, counter, url);

        Assert.Equal(one, many);
        Assert.InRange(many, 1, 4);
    }

    [Fact]
    public async Task Opening_an_exchange_runs_the_same_queries_for_a_short_or_long_learning_agreement()
    {
        var (factory, counter) = Counted();
        await using var _ = factory;
        var (partner, courses) = await NewPartner();
        var coordinator = await NewUser(UserRole.Coordinator);
        var student = await NewUser();
        var client = await LoggedIn(factory, await Email(student));

        async Task<int[]> OpenExchange(Guid exchange) =>
        [
            await Queries(client, counter, $"/api/exchanges/{exchange}"),
            await Queries(client, counter, $"/api/exchanges/{exchange}/learning-agreement"),
            await Queries(client, counter, $"/api/exchanges/{exchange}/recognition"),
            await Queries(client, counter, $"/api/exchanges/{exchange}/mapping-scheme"),
            await Queries(client, counter, $"/api/exchanges/{exchange}/learning-agreement/versions"),
        ];

        var shortLa = await NewExchange(student, partner, coordinator);
        await SaveLa(shortLa, student, AtExchange(Slot1, courses["A"], 5));
        await Approve(shortLa, coordinator);

        var longLa = await NewExchange(student, partner, coordinator);
        await SaveLa(longLa, student, AtExchange(Slot1, courses["A"], 5), AtExchange(Slot2, courses["B"], 5),
            AtExchange(Slot3, courses["C"], 5), AtExchange(Slot3, courses["D"], 1));
        await Approve(longLa, coordinator);
        await Reopen(longLa, coordinator);
        await SaveLa(longLa, student, AtExchange(Slot1, courses["A"], 5), AtExchange(Slot2, courses["B"], 5));
        await Approve(longLa, coordinator);

        var few = await OpenExchange(shortLa);
        var many = await OpenExchange(longLa);

        Assert.Equal(few, many);
        // exchange, LA, recognition, mapping scheme, versions: small indexed lookups, none per row.
        int[] budget = [2, 4, 7, 2, 2];
        Assert.All(many.Zip(budget), pair => Assert.InRange(pair.First, 1, pair.Second));
    }

    [Fact]
    public async Task The_admin_user_list_does_not_query_per_row()
    {
        var (factory, counter) = Counted();
        await using var _ = factory;
        var admin = await NewUser(UserRole.Admin);
        var client = await LoggedIn(factory, await Email(admin));

        var queries = await Queries(client, counter, "/api/admin/users?page=1&pageSize=50&search=user");
        Assert.InRange(queries, 1, 3);
    }
}
