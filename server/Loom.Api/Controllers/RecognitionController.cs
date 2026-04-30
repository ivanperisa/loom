using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.Recognition;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

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
    public async Task<IActionResult> SaveRecognition(
        Guid exchangeId,
        [FromBody] SaveRecognitionRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.SaveRecognitionAsync(exchangeId, GetCurrentUserId(), request, ct);
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

    [HttpPatch("entries/{entryId:guid}/recognized")]
    public async Task<IActionResult> SetEntryRecognized(
        Guid exchangeId,
        Guid entryId,
        [FromBody] SetEntryRecognizedRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.SetEntryRecognizedAsync(exchangeId, entryId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }
}
