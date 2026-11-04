using System.ComponentModel.DataAnnotations;

namespace Loom.Api.Options;

public sealed class FrontendOptions
{
    public const string Section = "Frontend";

    /// <summary>Where the SPA lives; login redirects back here.</summary>
    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;
}

public sealed class GoogleOptions
{
    public const string Section = "Google";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
