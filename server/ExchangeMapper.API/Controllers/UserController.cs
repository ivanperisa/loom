using ExchangeMapper.Application.DTOs.User;
using ExchangeMapper.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMapper.API.Controllers;

[Route("api/users")]
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

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var result = await userService.UpdateProfileAsync(GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }
}
