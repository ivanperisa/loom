using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMapper.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(IUserService userService) : ApiController
{
    [HttpPost("make-coordinator")]
    public async Task<IActionResult> MakeCoordinator([FromBody] MakeCoordinatorRequest request, CancellationToken ct)
    {
        var result = await userService.MakeCoordinatorAsync(request.UserId, ct);
        return Match(result, _ => Ok());
    }
}
