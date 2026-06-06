using Loom.Application.DTOs.Coordinator;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("[controller]")]
[Authorize]
public class CoordinatorController(IUserService userService, IExchangeService exchangeService) : ApiController
{
    [HttpGet("students")]
    public async Task<IActionResult> GetMyStudents(CancellationToken ct)
    {
        var result = await userService.GetMyStudentsAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPost("students")]
    public async Task<IActionResult> CreatePlaceholderStudent([FromBody] CreatePlaceholderStudentRequest request, CancellationToken ct)
    {
        var result = await userService.CreatePlaceholderStudentAsync(GetCurrentUserId(), request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetMyStudents), value));
    }

    [HttpGet("students/exchanges")]
    public async Task<IActionResult> GetMyStudentsExchanges(CancellationToken ct)
    {
        var result = await exchangeService.GetMyStudentsExchangesAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }
}
