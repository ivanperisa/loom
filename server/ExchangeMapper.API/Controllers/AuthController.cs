using System.Security.Claims;
using ExchangeMapper.API.Extensions;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Application.Mappers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExchangeMapper.API.Controllers;

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
        var clientId = configuration["Google:ClientId"];
        var clientSecret = configuration["Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return Problem(
                detail: "Google OAuth is not configured. Set Google:ClientId and Google:ClientSecret.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Configuration Error",
                extensions: new Dictionary<string, object?> { ["code"] = "CONFIGURATION_ERROR" });
        }

        var frontendTarget = configuration.BuildFrontendUrl(returnUrl);
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(frontendTarget);
        }

        var authProperties = new AuthenticationProperties
        {
            RedirectUri = frontendTarget
        };

        return Challenge(authProperties, "GoogleOidc");
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;

        if (sub is null)
        {
            return Ok(UserMapper.ToUnauthenticatedDto());
        }

        var user = await userService.GetByExternalIdWithDetailsAsync(sub, ct);
        if (user.IsError)
        {
            return Ok(UserMapper.ToUnauthenticatedDto());
        }

        return Ok(user.Value.ToAuthMeResponseDto());
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var authProperties = new AuthenticationProperties
        {
            RedirectUri = configuration.BuildFrontendUrl("/")
        };

        return SignOut(authProperties, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
