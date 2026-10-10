using Loom.Application.Common.Caching;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Loom.Application.Features.Catalog;

/// <summary>Home institution, programmes and profiles: read-mostly reference data, cached.</summary>
public sealed class HomeCatalogService(IAppDbContext db, HybridCache cache)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new() { Expiration = CacheTags.ReferenceDataTtl };
    private static readonly string[] Tags = [CacheTags.HomeCatalog];

    public async Task<List<InstitutionResponse>> GetHomeInstitutionsAsync(CancellationToken ct) =>
        await cache.GetOrCreateAsync("home-institutions", async token =>
            await db.Institutions
                .AsNoTracking()
                .Where(i => i.Type == InstitutionType.Home)
                .OrderBy(i => i.Name)
                .Select(i => new InstitutionResponse(i.Id, i.Name, i.NameHr, i.Country, i.City, i.ErasmusCode))
                .ToListAsync(token),
            CacheOptions, Tags, ct);

    public async Task<List<HomeProgramResponse>> GetHomeProgramsAsync(CancellationToken ct) =>
        await cache.GetOrCreateAsync("home-programs", async token =>
            await db.HomePrograms
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new HomeProgramResponse(p.Id, p.Name, p.NameEn, p.Level, p.DurationSemesters,
                    p.Profiles.OrderBy(x => x.Id).Select(x => new HomeProfileResponse(x.Id, x.Name, x.NameEn)).ToList()))
                .ToListAsync(token),
            CacheOptions, Tags, ct);
}
