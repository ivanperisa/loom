using Loom.Application.Common.Caching;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Loom.Application.Features.Coordination;

/// <summary>Everyone a student can pick as coordinator (cached; cleared when users change).</summary>
public sealed class CoordinatorDirectoryService(IAppDbContext db, HybridCache cache)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new() { Expiration = CacheTags.ReferenceDataTtl };

    public async Task<List<CoordinatorOptionResponse>> ListAsync(CancellationToken ct) =>
        await cache.GetOrCreateAsync("coordinators", async token =>
            await db.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Coordinator || u.Role == UserRole.Admin)
                .OrderBy(u => u.Name)
                .Select(u => new CoordinatorOptionResponse(u.Id, u.Name))
                .ToListAsync(token),
            CacheOptions, [CacheTags.Coordinators], ct);
}
