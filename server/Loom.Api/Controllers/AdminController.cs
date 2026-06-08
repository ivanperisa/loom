using Loom.Application.DTOs.Admin;
using Loom.Application.Interfaces.Services;
using Loom.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(IAdminService adminService) : ApiController
{
    #region Users

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var result = await adminService.GetAllUsersAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    #endregion

    #region Coordinator Requests & Whitelist

    [HttpGet("coordinator-requests")]
    public async Task<IActionResult> GetCoordinatorRequests(CancellationToken ct)
    {
        var result = await adminService.GetCoordinatorRequestsAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPatch("users/{userId:int}/make-coordinator")]
    public async Task<IActionResult> MakeCoordinator(int userId, CancellationToken ct)
    {
        var result = await adminService.MakeCoordinatorAsync(GetCurrentUserId(), userId, ct);
        return Match(result, Ok);
    }

    [HttpPatch("users/{userId:int}/reject-coordinator-request")]
    public async Task<IActionResult> RejectCoordinatorRequest(int userId, CancellationToken ct)
    {
        var result = await adminService.RejectCoordinatorRequestAsync(GetCurrentUserId(), userId, ct);
        return Match(result, Ok);
    }

    [HttpPatch("users/{userId:int}/remove-coordinator")]
    public async Task<IActionResult> RemoveCoordinator(int userId, CancellationToken ct)
    {
        var result = await adminService.RemoveCoordinatorAsync(GetCurrentUserId(), userId, ct);
        return Match(result, Ok);
    }

    [HttpGet("coordinator-whitelist")]
    public async Task<IActionResult> GetCoordinatorWhitelist(CancellationToken ct)
    {
        var result = await adminService.GetCoordinatorWhitelistAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPost("coordinator-whitelist")]
    public async Task<IActionResult> AddToCoordinatorWhitelist([FromBody] AddToWhitelistRequest request, CancellationToken ct)
    {
        var result = await adminService.AddToCoordinatorWhitelistAsync(GetCurrentUserId(), request.Email, ct);
        return Match(result, entry => CreatedAtAction(nameof(GetCoordinatorWhitelist), entry));
    }

    [HttpDelete("coordinator-whitelist/{email}")]
    public async Task<IActionResult> RemoveFromCoordinatorWhitelist(string email, CancellationToken ct)
    {
        var result = await adminService.RemoveFromCoordinatorWhitelistAsync(GetCurrentUserId(), Uri.UnescapeDataString(email), ct);
        return Match(result, _ => NoContent());
    }

    #endregion
}
