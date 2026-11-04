using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Loom.IntegrationTests;

/// <summary>
/// The OpenAPI document is the client's contract: <c>client/openapi.json</c> is generated from it, and the client's
/// TypeScript types from that file (<c>pnpm api:types</c>). This fails when the API changed and the file did not.
/// Update it with <c>LOOM_UPDATE_OPENAPI=1 dotnet test --filter ApiContract</c>.
/// </summary>
public class ApiContractTests(DatabaseFixture fixture) : HttpTest(fixture)
{
    [Fact]
    public async Task The_committed_openapi_document_is_up_to_date()
    {
        await using var factory = Factory();
        var json = await factory.CreateClient().GetStringAsync("/swagger/v1/swagger.json", Ct);
        var current = Normalize(json);

        var path = Path.Combine(RepositoryRoot(), "client", "openapi.json");
        if (Environment.GetEnvironmentVariable("LOOM_UPDATE_OPENAPI") == "1")
        {
            await File.WriteAllTextAsync(path, current, Ct);
            return;
        }

        Assert.True(File.Exists(path), $"{path} is missing. Run: LOOM_UPDATE_OPENAPI=1 dotnet test --filter ApiContract");
        Assert.True(current == await File.ReadAllTextAsync(path, Ct),
            "The API changed but client/openapi.json did not. Run: LOOM_UPDATE_OPENAPI=1 dotnet test --filter ApiContract, then pnpm api:types in client/.");
    }

    /// <summary>Stable formatting (indented, no server URL), so the file only changes when the contract does.</summary>
    private static string Normalize(string json)
    {
        var document = JsonNode.Parse(json)!.AsObject();
        document.Remove("servers");
        return document.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Loom.slnx"))) return dir.FullName;
        throw new InvalidOperationException("Loom.slnx not found above the test output.");
    }
}
