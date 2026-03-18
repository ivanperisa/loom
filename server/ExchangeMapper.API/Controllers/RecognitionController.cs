using ExchangeMapper.Application.DTOs.Exchange;
using ExchangeMapper.Application.DTOs.Recognition;
using ExchangeMapper.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMapper.API.Controllers;

[Route("api/exchanges/{exchangeId:guid}/recognition")]
[Authorize]
public class RecognitionController(IRecognitionService recognitionService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetOrCreate(Guid exchangeId, CancellationToken ct)
    {
        var result = await recognitionService.GetOrCreateRecognitionAsync(exchangeId, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut("entries")]
    public async Task<IActionResult> UpsertEntry(
        Guid exchangeId,
        [FromBody] UpsertRecognitionEntryRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.UpsertEntryAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(
        Guid exchangeId,
        [FromBody] UpdateExchangeStatusRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.UpdateRecognitionStatusAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }
}
