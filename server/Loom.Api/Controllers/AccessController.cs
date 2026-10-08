using System.Security.Claims;
using Loom.Api.Extensions;
using Loom.Application.Features.Exchanges;
using Loom.Application.Features.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>
/// Opening an access link. The token travels in a POST body (never in an API URL, so it stays out of logs)
/// and is swapped for the guest cookie or, for a signed-in student, claimed for their account.
/// </summary>
[Route("api/access")]
public class AccessController(AccessLinkService accessLinks, PlaceholderClaimService claims) : ApiController
{
    /// <summary>Starts a guest session for the link's exchange.</summary>
    [AllowAnonymous]
    [HttpPost("session")]
    public async Task<IActionResult> OpenSession([FromBody] AccessTokenRequest request, CancellationToken ct)
    {
        var session = await accessLinks.OpenAsync(request.Token, ct);
        if (session.IsError) return session.Errors.ToProblemDetails(this);

        var identity = new ClaimsIdentity([new Claim(AuthenticationSetup.GuestLinkClaim, session.Value.LinkId.ToString())], AuthenticationSetup.GuestScheme);
        await HttpContext.SignInAsync(AuthenticationSetup.GuestScheme, new ClaimsPrincipal(identity));
        return Ok(new { exchangeGuid = session.Value.ExchangeGuid });
    }

    [AllowAnonymous]
    [HttpDelete("session")]
    public async Task<IActionResult> CloseSession()
    {
        await HttpContext.SignOutAsync(AuthenticationSetup.GuestScheme);
        return NoContent();
    }

    /// <summary>For a signed-in user: is this their exchange already, or can they claim it?</summary>
    [Authorize]
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] AccessTokenRequest request, CancellationToken ct) =>
        Match(await accessLinks.PreviewAsync(request.Token, ct), Ok);

    /// <summary>A signed-in student takes over the placeholder (all its exchanges); the link stops working.</summary>
    [Authorize]
    [HttpPost("claim")]
    public async Task<IActionResult> Claim([FromBody] AccessTokenRequest request, CancellationToken ct)
    {
        var result = await claims.ClaimAsync(request.Token, ct);
        if (!result.IsError) await HttpContext.SignOutAsync(AuthenticationSetup.GuestScheme);
        return Match(result, Ok);
    }
}
