using System.Security.Claims;
using Loom.Api.Extensions;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Security;
using Loom.Application.Features.Exchanges;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Loom.Api.Filters;

/// <summary>
/// Decides who is acting on an exchange-scoped controller (routes with <c>{exchangeGuid}</c>):
/// a signed-in user, or, on <see cref="AllowGuestAttribute"/> actions, a guest whose access link opens this exchange.
/// The guest's link is re-checked on every request, so revoking or claiming it cuts access at once.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExchangeActorAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var actor = http.RequestServices.GetRequiredService<CurrentActor>();

        if (!actor.IsAuthenticated)
        {
            var error = await AuthenticateGuestAsync(context, actor);
            if (error is not null)
            {
                context.Result = error;
                return;
            }
        }

        await next();
    }

    private static async Task<IActionResult?> AuthenticateGuestAsync(ActionExecutingContext context, CurrentActor actor)
    {
        var http = context.HttpContext;
        var allowsGuest = context.ActionDescriptor.EndpointMetadata.OfType<AllowGuestAttribute>().Any();
        if (!allowsGuest) return new ChallengeResult();

        var guest = await http.AuthenticateAsync(AuthenticationSetup.GuestScheme);
        if (!guest.Succeeded || !int.TryParse(guest.Principal.FindFirstValue(AuthenticationSetup.GuestLinkClaim), out var linkId))
            return new ChallengeResult();

        var session = await http.RequestServices.GetRequiredService<AccessLinkService>().ResumeAsync(linkId, http.RequestAborted);
        if (session.IsError)
        {
            await http.SignOutAsync(AuthenticationSetup.GuestScheme);
            return new ChallengeResult();
        }

        if (context.ActionArguments.TryGetValue("exchangeGuid", out var routeGuid) && routeGuid is Guid guid && guid != session.Value.ExchangeGuid)
            return new[] { CommonErrors.AccessDenied }.ToProblemDetails((ControllerBase)context.Controller);

        actor.SetGuest(session.Value.StudentId, session.Value.ExchangeId);
        return null;
    }
}
