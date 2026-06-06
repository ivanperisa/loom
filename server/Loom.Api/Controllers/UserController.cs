using Loom.Application.DTOs.User;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("users")]
[Authorize]
public class UserController(IUserService userService) : ApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await userService.GetCurrentUserAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPost("me/onboarding")]
    public async Task<IActionResult> CompleteOnboarding(
        [FromBody] CompleteOnboardingRequest request,
        CancellationToken ct)
    {
        var result = await userService.CompleteOnboardingAsync(GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPost("me/coordinator-request")]
    public async Task<IActionResult> RequestCoordinatorRole(CancellationToken ct)
    {
        var result = await userService.RequestCoordinatorRoleAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var result = await userService.UpdateProfileAsync(GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }
}
