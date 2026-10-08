using Loom.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/users")]
[Authorize]
public class UserController(AccountService accounts) : ApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct) =>
        Match(await accounts.GetMeAsync(ct), Ok);

    [HttpPost("me/onboarding")]
    public async Task<IActionResult> CompleteOnboarding([FromBody] CompleteOnboardingRequest request, CancellationToken ct) =>
        Match(await accounts.CompleteOnboardingAsync(request, ct), Ok);

    [HttpPost("me/coordinator-request")]
    public async Task<IActionResult> RequestCoordinatorRole(CancellationToken ct) =>
        Match(await accounts.RequestCoordinatorRoleAsync(ct), Ok);

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct) =>
        Match(await accounts.UpdateProfileAsync(request, ct), Ok);
}
