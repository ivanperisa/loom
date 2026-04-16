namespace Loom.Api.Extensions;

public static class FrontendUrlExtensions
{
    public static string BuildFrontendUrl(this IConfiguration configuration, string? returnPath)
    {
        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var normalizedPath = NormalizeRelativePath(returnPath);
        return $"{frontendBaseUrl.TrimEnd('/')}{normalizedPath}";
    }

    public static string NormalizeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith('/') || normalized.StartsWith("//"))
        {
            return "/";
        }

        return normalized;
    }
}
