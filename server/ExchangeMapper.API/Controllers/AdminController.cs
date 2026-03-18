using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMapper.API.Controllers;

[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(IUserService userService) : ApiController
{
    [HttpPatch("users/{userId:guid}/make-coordinator")]
    public async Task<IActionResult> MakeCoordinator(Guid userId, CancellationToken ct)
    {
        var result = await userService.MakeCoordinatorAsync(GetCurrentUserId(), userId, ct);
        return Match(result, Ok);
    }
}
