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
public class ApiTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private readonly DatabaseFixture _fixture = fixture;

    private WebApplicationFactory<Program> Factory(string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment(environment);
            host.UseSetting("ConnectionStrings:DefaultConnection", _fixture.ConnectionString);
            host.UseSetting("DevAuth:Enabled", environment == "Development" ? "true" : "false");
            host.UseSetting("Frontend:BaseUrl", "http://localhost:5173");
            if (environment != "Development")
            {
                host.UseSetting("Google:ClientId", "test-client");
                host.UseSetting("Google:ClientSecret", "test-secret");
            }
        });

    private static async Task<HttpClient> LoggedIn(WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/dev/login", new { email }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return client;
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [Fact]
    public async Task Exchange_routes_need_a_login_or_a_valid_access_link()
    {
        await using var factory = Factory();
        var (partner, _) = await NewPartner();
        var student = await NewUser();
        var registered = await NewExchange(student, partner, coordinator: null);
        var placeholder = await Db(async db =>
        {
            var user = new User { ExternalId = $"ph:{Guid.NewGuid():N}", Email = "", Name = "Placeholder", Role = UserRole.Student, IsOnboarded = true };
            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);
            return user.Id;
        });
        var guestExchange = await NewExchange(placeholder, partner, coordinator: null);
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/exchanges/{registered}/learning-agreement", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/exchanges/access/{guestExchange}/learning-agreement", Ct)).StatusCode);

        var denied = await anonymous.GetAsync($"/api/exchanges/access/{registered}/learning-agreement", Ct);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("ACCESS_DENIED", await ErrorCode(denied));

        var missing = await anonymous.GetAsync($"/api/exchanges/access/{Guid.NewGuid()}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("EXCHANGE_NOT_FOUND", await ErrorCode(missing));
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
