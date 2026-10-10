using System.Text.RegularExpressions;

namespace Loom.Application.Common;

public static partial class Text
{
    /// <summary>Trimmed value, or null for null/blank input.</summary>
    public static string? NullIfBlank(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValidJmbag(string? value) => value is not null && JmbagPattern().IsMatch(value);

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex JmbagPattern();
}
