using Loom.Application.Features.Admin;
using Loom.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(
    AdminUserService users,
    CoordinatorRequestService coordinatorRequests,
    CoordinatorWhitelistService whitelist) : ApiController
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] UserListQuery query, CancellationToken ct) =>
        Ok(await users.ListAsync(query, ct));

    [HttpPut("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] AdminUpdateUserRequest request, CancellationToken ct) =>
        Match(await users.UpdateAsync(userId, request, ct), Ok);

    [HttpPatch("users/{userId:int}/role")]
    public async Task<IActionResult> SetUserRole(int userId, [FromBody] AdminSetRoleRequest request, CancellationToken ct) =>
        Match(await users.SetRoleAsync(userId, request.Role, ct), Ok);

    [HttpGet("coordinator-requests")]
    public async Task<IActionResult> GetCoordinatorRequests(CancellationToken ct) =>
        Ok(await coordinatorRequests.ListPendingAsync(ct));

    [HttpPatch("users/{userId:int}/reject-coordinator-request")]
    public async Task<IActionResult> RejectCoordinatorRequest(int userId, CancellationToken ct) =>
        Match(await coordinatorRequests.RejectAsync(userId, ct), Ok);

    [HttpGet("coordinator-whitelist")]
    public async Task<IActionResult> GetCoordinatorWhitelist(CancellationToken ct) =>
        Ok(await whitelist.ListAsync(ct));

    [HttpPost("coordinator-whitelist")]
    public async Task<IActionResult> AddToCoordinatorWhitelist([FromBody] AddToWhitelistRequest request, CancellationToken ct) =>
        Match(await whitelist.AddAsync(request.Email, ct), entry => CreatedAtAction(nameof(GetCoordinatorWhitelist), entry));

    [HttpDelete("coordinator-whitelist/{email}")]
    public async Task<IActionResult> RemoveFromCoordinatorWhitelist(string email, CancellationToken ct) =>
        Match(await whitelist.RemoveAsync(Uri.UnescapeDataString(email), ct), _ => NoContent());
}
