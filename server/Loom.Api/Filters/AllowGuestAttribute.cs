using Microsoft.AspNetCore.Authorization;

namespace Loom.Api.Filters;

/// <summary>
/// Someone with a guest session (opened from an access link) may call this action too, for the one exchange
/// their link opens. Skips the login requirement; <see cref="ExchangeActorAttribute"/> does the checking.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowGuestAttribute : Attribute, IAllowAnonymous;
