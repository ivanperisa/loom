using Loom.Application.Common.Querying;
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
    public async Task<ActionResult<PagedResponse<UserListResponse>>> GetUsers([FromQuery] UserListQuery query, CancellationToken ct) =>
        Ok(await users.ListAsync(query, ct));

    [HttpPut("users/{userId:int}")]
    public async Task<ActionResult<UserListResponse>> UpdateUser(int userId, [FromBody] AdminUpdateUserRequest request, CancellationToken ct) =>
        Match(await users.UpdateAsync(userId, request, ct), Ok);

    [HttpPatch("users/{userId:int}/role")]
    public async Task<ActionResult<UserListResponse>> SetUserRole(int userId, [FromBody] AdminSetRoleRequest request, CancellationToken ct) =>
        Match(await users.SetRoleAsync(userId, request.Role, ct), Ok);

    [HttpGet("coordinator-requests")]
    public async Task<ActionResult<PagedResponse<CoordinatorRequestResponse>>> GetCoordinatorRequests([FromQuery] ListQuery query, CancellationToken ct) =>
        Ok(await coordinatorRequests.ListPendingAsync(query, ct));

    /// <summary>Approve or reject a student's request to become a coordinator.</summary>
    [HttpPatch("coordinator-requests/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DecideCoordinatorRequest(int userId, [FromBody] DecideCoordinatorRequestRequest request, CancellationToken ct) =>
        Match(await coordinatorRequests.DecideAsync(userId, request.Status, ct), _ => NoContent());

    [HttpGet("coordinator-whitelist")]
    public async Task<ActionResult<PagedResponse<CoordinatorWhitelistEntryResponse>>> GetCoordinatorWhitelist([FromQuery] ListQuery query, CancellationToken ct) =>
        Ok(await whitelist.ListAsync(query, ct));

    [HttpPost("coordinator-whitelist")]
    [ProducesResponseType<CoordinatorWhitelistEntryResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelist([FromBody] AddToWhitelistRequest request, CancellationToken ct) =>
        Match(await whitelist.AddAsync(request.Email, ct), entry => CreatedAtAction(nameof(GetCoordinatorWhitelist), entry));

    [HttpDelete("coordinator-whitelist/{email}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFromCoordinatorWhitelist(string email, CancellationToken ct) =>
        Match(await whitelist.RemoveAsync(Uri.UnescapeDataString(email), ct), _ => NoContent());
}
