using Loom.Application.DTOs.Admin;
using Loom.Application.Interfaces.Services;
using Loom.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(IUserService userService) : ApiController
{
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var result = await userService.GetAllUsersAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpGet("coordinator-requests")]
    public async Task<IActionResult> GetCoordinatorRequests(CancellationToken ct)
    {
        var result = await userService.GetCoordinatorRequestsAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPatch("users/{userId:guid}/make-coordinator")]
    public async Task<IActionResult> MakeCoordinator(Guid userId, CancellationToken ct)
    {
        var result = await userService.MakeCoordinatorAsync(GetCurrentUserId(), userId, ct);
        return Match(result, Ok);
    }

    [HttpPatch("users/{userId:guid}/reject-coordinator-request")]
    public async Task<IActionResult> RejectCoordinatorRequest(Guid userId, CancellationToken ct)
    {
        var result = await userService.RejectCoordinatorRequestAsync(GetCurrentUserId(), userId, ct);
        return Match(result, Ok);
    }

    [HttpPatch("users/{userId:guid}/remove-coordinator")]
    public async Task<IActionResult> RemoveCoordinator(Guid userId, CancellationToken ct)
    {
        var result = await userService.RemoveCoordinatorAsync(GetCurrentUserId(), userId, ct);
        return Match(result, Ok);
    }

    [HttpGet("coordinator-whitelist")]
    public async Task<IActionResult> GetCoordinatorWhitelist(CancellationToken ct)
    {
        var result = await userService.GetCoordinatorWhitelistAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPost("coordinator-whitelist")]
    public async Task<IActionResult> AddToCoordinatorWhitelist([FromBody] AddToWhitelistRequest request, CancellationToken ct)
    {
        var result = await userService.AddToCoordinatorWhitelistAsync(GetCurrentUserId(), request.Email, ct);
        return Match(result, entry => CreatedAtAction(nameof(GetCoordinatorWhitelist), entry));
    }

    [HttpDelete("coordinator-whitelist/{email}")]
    public async Task<IActionResult> RemoveFromCoordinatorWhitelist(string email, CancellationToken ct)
    {
        var result = await userService.RemoveFromCoordinatorWhitelistAsync(GetCurrentUserId(), Uri.UnescapeDataString(email), ct);
        return Match(result, _ => NoContent());
    }
}
