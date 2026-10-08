using Loom.Api.Extensions;
using Loom.Application.Common.Security;
using Loom.Application.Features.Exchanges;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Loom.Api.Filters;

/// <summary>
/// For exchange-scoped controllers whose actions are reachable two ways:
/// <c>/api/exchanges/{guid}/…</c> (signed-in student or coordinator) and
/// <c>/api/exchanges/access/{guid}/…</c> (access link, acting as the placeholder student).
/// Actions carry both routes and <c>[AllowAnonymous]</c>; this filter decides who the actor is,
/// and rejects anonymous calls on the normal route.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExchangeActorAttribute : Attribute, IAsyncActionFilter
{
    public const string GuestRoutePrefix = "api/exchanges/access/";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var actor = services.GetRequiredService<CurrentActor>();
        var template = (context.ActionDescriptor.AttributeRouteInfo?.Template ?? string.Empty).TrimStart('/');

        if (template.StartsWith(GuestRoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var exchangeGuid = (Guid)context.ActionArguments["exchangeGuid"]!;
            var guest = await services.GetRequiredService<GuestAccessService>()
                .ResolvePlaceholderStudentAsync(exchangeGuid, context.HttpContext.RequestAborted);
            if (guest.IsError)
            {
                context.Result = guest.Errors.ToProblemDetails((ControllerBase)context.Controller);
                return;
            }
            actor.Set(guest.Value, isGuest: true);
        }
        else if (!actor.IsAuthenticated)
        {
            context.Result = new ChallengeResult();   // same 401 as every other unauthenticated call
            return;
        }

        await next();
    }
}
