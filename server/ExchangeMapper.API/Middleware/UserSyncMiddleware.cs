using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace ExchangeMapper.API.Middleware;

public class UserSyncMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private record CachedUser(Guid Id, UserRole Role);

    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var sub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value
            ?? context.User.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(email))
        {
            await next(context);
            return;
        }

        var cacheKey = $"usersync:{sub}";

        if (!cache.TryGetValue(cacheKey, out CachedUser? cached))
        {
            var name = context.User.FindFirst(ClaimTypes.Name)?.Value
                ?? context.User.FindFirst("name")?.Value
                ?? email;

            var syncResult = await userService.SyncUserAsync(sub, email, name);
            if (syncResult.IsError)
            {
                await next(context);
                return;
            }

            cached = new CachedUser(syncResult.Value.Id, syncResult.Value.Role);
            cache.Set(cacheKey, cached, CacheDuration);
        }

        if (context.User.Identity is not ClaimsIdentity identity)
        {
            await next(context);
            return;
        }

        if (!identity.HasClaim(c => c.Type == "userId"))
            identity.AddClaim(new Claim("userId", cached!.Id.ToString()));

        if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
            identity.AddClaim(new Claim(ClaimTypes.Role, cached!.Role.ToString()));

        await next(context);
    }
}