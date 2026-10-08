using System.Linq.Expressions;
using Loom.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Common.Querying;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;

    /// <summary>search → count → sort (+ Id tiebreaker) → page → project, all in SQL.</summary>
    public static async Task<PagedResponse<TResponse>> ToPageAsync<TEntity, TResponse>(
        this IQueryable<TEntity> query, ListSpec<TEntity, TResponse> spec, ListQuery listQuery, CancellationToken ct)
        where TEntity : EntityBase
    {
        var filtered = spec.ApplySearch(query, listQuery.SearchTerm);
        var totalCount = await filtered.CountAsync(ct);
        var items = await spec.ApplySort(filtered, listQuery)
            .Skip(listQuery.Skip)
            .Take(listQuery.SafePageSize)
            .Select(spec.Projection)
            .ToListAsync(ct);

        return new PagedResponse<TResponse>(items, listQuery.SafePage, listQuery.SafePageSize, totalCount);
    }
}

public static class SoftDeleteQueryExtensions
{
    /// <summary>Explicit (not a global filter) so navigations to soft-deleted rows keep loading.</summary>
    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query, bool includeDeleted) where T : Domain.Common.ISoftDeletable =>
        includeDeleted ? query : query.Where(x => !x.IsDeleted);
}
