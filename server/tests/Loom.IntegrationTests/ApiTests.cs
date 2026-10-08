using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.IntegrationTests;

/// <summary>Through the real HTTP pipeline: authentication, the exchange actor filter, error shapes, health.</summary>
public class ApiTests(DatabaseFixture fixture) : HttpTest(fixture)
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Exchange_routes_need_a_login()
    {
        await using var factory = Factory();
        var (partner, _) = await NewPartner();
        var exchange = await NewExchange(await NewUser(), partner, coordinator: null);
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/exchanges/{exchange}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/exchanges/{exchange}/learning-agreement", Ct)).StatusCode);
        // The old guest routes are gone: the exchange GUID is an id, not a secret.
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/exchanges/access/{exchange}/learning-agreement", Ct)).StatusCode);
    }

    [Fact]
    public async Task Students_only_see_their_own_exchange()
    {
        await using var factory = Factory();
        var (partner, _) = await NewPartner();
        var owner = await NewUser();
        var exchange = await NewExchange(owner, partner, coordinator: null);
        var ownerEmail = await Db(db => db.Users.Where(u => u.Id == owner).Select(u => u.Email).SingleAsync(Ct));
        var otherEmail = await Db(async db => (await db.Users.FindAsync([await NewUser()], Ct))!.Email);

        var asOwner = await LoggedIn(factory, ownerEmail);
        var asOther = await LoggedIn(factory, otherEmail);

        Assert.Equal(HttpStatusCode.OK, (await asOwner.GetAsync($"/api/exchanges/{exchange}", Ct)).StatusCode);
        var denied = await asOther.GetAsync($"/api/exchanges/{exchange}", Ct);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("ACCESS_DENIED", await ErrorCode(denied));
    }

    [Fact]
    public async Task Dev_login_does_not_exist_outside_development()
    {
        await using var factory = Factory("Production");
        var response = await factory.CreateClient().PostAsJsonAsync("/auth/dev/login", new { email = "admin@loom.dev" }, Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Without_a_session_the_api_answers_401_even_with_google_configured()
    {
        await using var factory = Factory("Production");
        var response = await factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
            .GetAsync("/api/exchanges/mine", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);   // not a 302 to accounts.google.com
    }

    [Fact]
    public async Task Readiness_checks_the_database()
    {
        await using var factory = Factory();
        var response = await factory.CreateClient().GetAsync("/healthz/ready", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_writes_to_the_same_document_are_detected()
    {
        var (partner, _) = await NewPartner();
        var exchange = await NewExchange(await NewUser(), partner, coordinator: null);

        await using var first = DatabaseFixture.BuildServices(_fixture.ConnectionString);
        await using var second = DatabaseFixture.BuildServices(_fixture.ConnectionString);
        await using var scopeA = first.CreateAsyncScope();
        await using var scopeB = second.CreateAsyncScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();

        var a = await dbA.LearningAgreements.SingleAsync(l => l.Exchange.Guid == exchange, Ct);
        var b = await dbB.LearningAgreements.SingleAsync(l => l.Exchange.Guid == exchange, Ct);
        a.Message = "first";
        await dbA.SaveChangesAsync(Ct);
        b.Message = "second";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync(Ct));
    }
}
