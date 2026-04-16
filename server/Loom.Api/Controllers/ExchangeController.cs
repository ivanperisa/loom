using Loom.Application.DTOs.CourseSlot;
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

    [HttpPatch("{exchangeId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid exchangeId,
        [FromBody] UpdateExchangeStatusRequest request,
        CancellationToken ct)
    {
        var result = await exchangeService.UpdateExchangeStatusAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpGet("{exchangeId:guid}/learning-agreement")]
    public async Task<IActionResult> GetLearningAgreement(Guid exchangeId, CancellationToken ct)
    {
        var result = await exchangeService.GetLearningAgreementAsync(exchangeId, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPost("{exchangeId:guid}/learning-agreement/slot-mode")]
    public async Task<IActionResult> SetSlotMode(Guid exchangeId, [FromBody] SetSlotModeRequest request, CancellationToken ct)
    {
        var result = await exchangeService.SetSlotModeAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPost("{exchangeId:guid}/learning-agreement/mappings")]
    public async Task<IActionResult> AddSlotMapping(Guid exchangeId, [FromBody] AddSlotMappingRequest request, CancellationToken ct)
    {
        var result = await exchangeService.AddSlotMappingAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpDelete("{exchangeId:guid}/learning-agreement/mappings")]
    public async Task<IActionResult> RemoveSlotMapping(Guid exchangeId, [FromBody] RemoveSlotMappingRequest request, CancellationToken ct)
    {
        var result = await exchangeService.RemoveSlotMappingAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpDelete("{exchangeId:guid}/learning-agreement/slot-state")]
    public async Task<IActionResult> RemoveSlotState(Guid exchangeId, [FromBody] RemoveSlotStateRequest request, CancellationToken ct)
    {
        var result = await exchangeService.RemoveSlotStateAsync(exchangeId, GetCurrentUserId(), request.CourseSlotId, ct);
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
