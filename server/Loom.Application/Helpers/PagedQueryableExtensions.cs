using System.Linq.Expressions;
using Loom.Application.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Helpers;

public static class PagedQueryableExtensions
{
    public static async Task<PagedResponse<TResponse>> ToPagedResponseAsync<TEntity, TKey, TResponse>(
        this IQueryable<TEntity> filtered,
        PagedRequest paging,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TKey>> tiebreaker,
        Func<TEntity, TResponse> map,
        CancellationToken ct = default)
    {
        var totalCount = await filtered.CountAsync(ct);
        var items = await orderBy(filtered)
            .ThenBy(tiebreaker)
            .Skip(paging.Skip)
            .Take(paging.SafePageSize)
            .ToListAsync(ct);

        return new PagedResponse<TResponse>(items.Select(map).ToList(), paging.SafePage, paging.SafePageSize, totalCount);
    }
}
