using Loom.Application.Interfaces.Services;
using Loom.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Loom.Api.Middleware;

public class UserSyncMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private record CachedUser(int Id, UserRole Role);

    public async Task InvokeAsync(HttpContext context, IUserSyncService userSyncService)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var externalId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value
            ?? context.User.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
        {
            await next(context);
            return;
        }

        var cacheKey = $"usersync:{externalId}";

        if (!cache.TryGetValue(cacheKey, out CachedUser? cached))
        {
            var name = context.User.FindFirst(ClaimTypes.Name)?.Value
                ?? context.User.FindFirst("name")?.Value
                ?? email;

            var syncResult = await userSyncService.SyncUserAsync(externalId, email, name);
            if (syncResult.IsError)
            {
                await next(context);
                return;
            }

            cached = new CachedUser(syncResult.Value.Id, syncResult.Value.Role);
            cache.Set(cacheKey, cached, CacheDuration);
        }

        var identity = (ClaimsIdentity)context.User.Identity!;

        var existingNameId = identity.FindFirst(ClaimTypes.NameIdentifier);
        if (existingNameId is not null)
            identity.RemoveClaim(existingNameId);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, cached!.Id.ToString()));

        var existingRole = identity.FindFirst(ClaimTypes.Role);
        if (existingRole is not null)
            identity.RemoveClaim(existingRole);
        identity.AddClaim(new Claim(ClaimTypes.Role, cached!.Role.ToString()));

        await next(context);
    }
}
