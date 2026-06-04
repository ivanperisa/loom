using Loom.Api.Extensions;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Loom.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(
    IConfiguration configuration,
    IUserService userService) : ApiController
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        var frontendTarget = configuration.BuildFrontendUrl(returnUrl);
        if (User.Identity?.IsAuthenticated == true)
        {
            return SignOut(
            new AuthenticationProperties { RedirectUri = $"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}"},
            CookieAuthenticationDefaults.AuthenticationScheme);
        }

        var authProperties = new AuthenticationProperties { RedirectUri = frontendTarget };
        authProperties.Parameters["prompt"] = "select_account";

        return Challenge(authProperties, "GoogleOidc");
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null)
        {
            return Ok(new { IsAuthenticated = false });
        }

        var result = await userService.GetCurrentUserAsync(userId.Value, ct);
        if (result.IsError)
        {
            return Ok(new { IsAuthenticated = false });
        }

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
