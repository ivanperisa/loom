using System.Runtime.CompilerServices;
using Loom.Application.Common.Caching;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;

namespace Loom.Infrastructure.Caching;

/// <summary>
/// Clears cached reference data whenever SaveChanges writes one of the entity types it is built from,
/// so no service has to remember to invalidate.
/// </summary>
public sealed class CacheInvalidationInterceptor(HybridCache cache) : SaveChangesInterceptor
{
    private static readonly Dictionary<Type, string> TagsByEntity = new()
    {
        [typeof(Institution)] = CacheTags.HomeCatalog,
        [typeof(HomeProgram)] = CacheTags.HomeCatalog,
        [typeof(HomeProfile)] = CacheTags.HomeCatalog,
        [typeof(User)] = CacheTags.Coordinators,
    };

    // Tags collected before saving, cleared after the save succeeded (per DbContext instance).
    private readonly ConditionalWeakTable<DbContext, HashSet<string>> _pending = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await InvalidateAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        InvalidateAsync(eventData.Context, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private void Collect(DbContext? context)
    {
        if (context is null) return;
        var tags = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => TagsByEntity.GetValueOrDefault(e.Metadata.ClrType))
            .OfType<string>()
            .ToHashSet();
        if (tags.Count > 0) _pending.AddOrUpdate(context, tags);
    }

    private async ValueTask InvalidateAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null || !_pending.TryGetValue(context, out var tags)) return;
        _pending.Remove(context);
        await cache.RemoveByTagAsync(tags, ct);
    }
}
