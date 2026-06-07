using Loom.Application.DTOs.Coordinator;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class CoordinatorController(ICoordinatorService coordinatorService) : ApiController
{
    [HttpGet("/api/coordinators")]
    public async Task<IActionResult> GetCoordinators(CancellationToken ct)
    {
        var result = await coordinatorService.GetCoordinatorsAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetMyStudents(CancellationToken ct)
    {
        var result = await coordinatorService.GetMyStudentsAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPost("students")]
    public async Task<IActionResult> CreatePlaceholderStudent([FromBody] CreatePlaceholderStudentRequest request, CancellationToken ct)
    {
        var result = await coordinatorService.CreatePlaceholderStudentAsync(GetCurrentUserId(), request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetMyStudents), value));
    }

    [HttpGet("students/exchanges")]
    public async Task<IActionResult> GetMyStudentsExchanges(CancellationToken ct)
    {
        var result = await coordinatorService.GetMyStudentsExchangesAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }
}
