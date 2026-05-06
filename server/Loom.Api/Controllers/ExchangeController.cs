using Loom.Application.DTOs.Exchange;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges")]
[Authorize]
public class ExchangeController(IExchangeService exchangeService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateExchange([FromBody] CreateExchangeRequest request, CancellationToken ct)
    {
        var result = await exchangeService.CreateExchangeAsync(GetCurrentUserId(), request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetExchange), new { exchangeId = value.Id }, value));
    }

    [HttpGet("{exchangeId:guid}")]
    public async Task<IActionResult> GetExchange(Guid exchangeId, CancellationToken ct)
    {
        var result = await exchangeService.GetExchangeAsync(exchangeId, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyExchanges(CancellationToken ct)
    {
        var result = await exchangeService.GetMyExchangesAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpDelete("{exchangeId:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid exchangeId, CancellationToken ct)
    {
        var result = await exchangeService.DeleteExchangeAsync(exchangeId, GetCurrentUserId(), ct);
        return Match(result, _ => NoContent());
    }

    [HttpGet("{exchangeId:guid}/snapshots")]
    public async Task<IActionResult> GetSnapshots(Guid exchangeId, CancellationToken ct)
    {
        var result = await exchangeService.GetSnapshotsAsync(exchangeId, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpGet("{exchangeId:guid}/snapshots/{snapshotId:guid}")]
    public async Task<IActionResult> GetSnapshot(Guid exchangeId, Guid snapshotId, CancellationToken ct)
    {
        var result = await exchangeService.GetSnapshotAsync(exchangeId, snapshotId, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut("{exchangeId:guid}/coordinator-message")]
    public async Task<IActionResult> UpdateCoordinatorMessage(Guid exchangeId, [FromBody] UpdateCoordinatorMessageRequest request, CancellationToken ct)
    {
        var result = await exchangeService.UpdateCoordinatorMessageAsync(exchangeId, GetCurrentUserId(), request.Message, ct);
        return Match(result, Ok);
    }

    [HttpGet("coordinator/students")]
    public async Task<IActionResult> GetMyStudents(CancellationToken ct)
    {
        var result = await exchangeService.GetMyStudentsExchangesAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }
}
