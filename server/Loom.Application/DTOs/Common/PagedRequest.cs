namespace Loom.Application.DTOs.Common;

public record PagedRequest
{
    private const int MaxPageSize = 200;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Search { get; init; }
    public string? SortDir { get; init; }

    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 25 : PageSize > MaxPageSize ? MaxPageSize : PageSize;
    public int Skip => (SafePage - 1) * SafePageSize;
}
