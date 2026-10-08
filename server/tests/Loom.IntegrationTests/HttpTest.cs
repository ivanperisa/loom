using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Loom.IntegrationTests;

/// <summary>Tests through the real HTTP pipeline (WebApplicationFactory) against the test database.</summary>
public abstract class HttpTest(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private readonly DatabaseFixture _fixture = fixture;

    protected WebApplicationFactory<Program> Factory(string environment = "Development") =>
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

    protected static async Task<HttpClient> LoggedIn(WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/dev/login", new { email }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return client;
    }

    protected static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
