using Loom.Application.Common.Security;
using Loom.Api.Extensions;
using Loom.Application.Features.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Loom.Api.Controllers;

[Route("[controller]")]
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
            new AuthenticationProperties { RedirectUri = $"{Request.PathBase}/auth/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}"},
            CookieAuthenticationDefaults.AuthenticationScheme);
        }

        var authProperties = new AuthenticationProperties { RedirectUri = frontendTarget };
        authProperties.Parameters["prompt"] = "select_account";

        return Challenge(authProperties, AuthenticationSetup.GoogleScheme);
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!actor.IsAuthenticated) return Ok(new { IsAuthenticated = false });

        var result = await accounts.GetAsync(actor.UserId, ct);
        return result.IsError ? Ok(new { IsAuthenticated = false }) : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
