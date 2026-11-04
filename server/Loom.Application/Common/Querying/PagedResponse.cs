namespace Loom.Application.Common.Querying;

public record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount, bool HasDeleted = false);
