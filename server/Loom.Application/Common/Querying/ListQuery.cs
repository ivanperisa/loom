namespace Loom.Application.Common.Querying;

/// <summary>
/// Paging, search and sorting parameters shared by every list endpoint (bound from the query string).
/// Sort with <c>sort=name</c> / <c>sort=-name</c>.
/// </summary>
public record ListQuery
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Search { get; init; }
    public string? Sort { get; init; }

    public int SafePage => Math.Max(Page, 1);
    public int SafePageSize => PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
    public int Skip => (int)Math.Min((long)(SafePage - 1) * SafePageSize, int.MaxValue);
    public string? SearchTerm => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

    public (string? Key, bool Descending) ResolveSort() =>
        string.IsNullOrWhiteSpace(Sort) ? (null, false) : (Sort.TrimStart('-'), Sort.StartsWith('-'));
}
