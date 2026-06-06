using Loom.Application.DTOs.Exchange;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("exchanges")]
[Authorize]
public class ExchangeController(IExchangeService exchangeService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateExchange([FromBody] CreateExchangeRequest request, CancellationToken ct)
    {
        var result = await exchangeService.CreateExchangeAsync(GetCurrentUserId(), request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetExchange), new { exchangeGuid = value.Guid }, value));
    }

    [HttpGet("{exchangeGuid:guid}")]
    public async Task<IActionResult> GetExchange(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await exchangeService.GetExchangeAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyExchanges(CancellationToken ct)
    {
        var result = await exchangeService.GetMyExchangesAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpDelete("{exchangeGuid:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await exchangeService.DeleteExchangeAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, _ => NoContent());
    }

    [HttpGet("{exchangeGuid:guid}/snapshots")]
    public async Task<IActionResult> GetSnapshots(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await exchangeService.GetSnapshotsAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpGet("{exchangeGuid:guid}/snapshots/{snapshotId:int}")]
    public async Task<IActionResult> GetSnapshot(Guid exchangeGuid, int snapshotId, CancellationToken ct)
    {
        var result = await exchangeService.GetSnapshotAsync(exchangeGuid, snapshotId, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut("{exchangeGuid:guid}/coordinator-message")]
    public async Task<IActionResult> UpdateCoordinatorMessage(Guid exchangeGuid, [FromBody] UpdateCoordinatorMessageRequest request, CancellationToken ct)
    {
        var result = await exchangeService.UpdateCoordinatorMessageAsync(exchangeGuid, GetCurrentUserId(), request.Message, ct);
        return Match(result, Ok);
    }

}
