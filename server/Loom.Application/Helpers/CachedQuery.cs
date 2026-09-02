using Microsoft.Extensions.Caching.Memory;

namespace Loom.Application.Helpers;

public class CachedQuery(IMemoryCache cache)
{
    public Task<T> GetOrCreateAsync<T>(string entityTag, string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        var version = cache.GetOrCreate(VersionKey(entityTag), entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return 0;
        });

        return cache.GetOrCreateAsync($"{entityTag}:{version}:{key}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory();
        })!;
    }

    public void BumpVersion(string entityTag)
    {
        var key = VersionKey(entityTag);
        var current = cache.TryGetValue(key, out int version) ? version : 0;
        cache.Set(key, current + 1, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
    }

    private static string VersionKey(string entityTag) => $"cachever:{entityTag}";
}
