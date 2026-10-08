using Loom.Api.Options;
using Microsoft.Extensions.Options;

namespace Loom.Api.Extensions;

/// <summary>Builds links back into the SPA; only relative paths are accepted (no open redirects).</summary>
public sealed class FrontendUrls(IOptions<FrontendOptions> options)
{
    public string Build(string? returnPath) => $"{options.Value.BaseUrl.TrimEnd('/')}{NormalizeRelativePath(returnPath)}";

    public static string NormalizeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var normalized = path.Trim();
        return !normalized.StartsWith('/') || normalized.StartsWith("//") ? "/" : normalized;
    }
}
