using Loom.Application.Common.Security;
using Loom.Api.Extensions;
using Loom.Application.Features.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Loom.Api.Controllers;

[Route("api/auth")]
public class AuthController(
    FrontendUrls frontend,
    AccountService accounts,
    ICurrentActor actor,
    IAuthenticationSchemeProvider schemes) : ApiController
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpGet("login")]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl = "/")
    {
        var frontendTarget = frontend.Build(returnUrl);

        // Local development without Google credentials: use the dev login on the landing page instead.
        if (await schemes.GetSchemeAsync(AuthenticationSetup.GoogleScheme) is null)
            return Redirect(frontend.Build("/"));

        if (User.Identity?.IsAuthenticated == true)
        {
            return SignOut(
            new AuthenticationProperties { RedirectUri = $"{Request.PathBase}/api/auth/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}"},
            CookieAuthenticationDefaults.AuthenticationScheme);
        }

        var authProperties = new AuthenticationProperties { RedirectUri = frontendTarget };
        authProperties.Parameters["prompt"] = "select_account";

        return Challenge(authProperties, AuthenticationSetup.GoogleScheme);
    }

    /// <summary>Whether someone is signed in, and who. Always 200, so an anonymous visit is not an error.</summary>
    [AllowAnonymous]
    [HttpGet("session")]
    public async Task<ActionResult<SessionResponse>> Session(CancellationToken ct)
    {
        if (!actor.IsAuthenticated) return new SessionResponse(false, null);

        var result = await accounts.GetAsync(actor.UserId, ct);
        return result.IsError ? new SessionResponse(false, null) : new SessionResponse(true, result.Value);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
