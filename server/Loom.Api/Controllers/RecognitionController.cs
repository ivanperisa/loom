using Loom.Application.DTOs.Recognition;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("exchanges/{exchangeGuid:guid}/recognition")]
[Authorize]
public class RecognitionController(IRecognitionService recognitionService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetOrCreate(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await recognitionService.GetOrCreateRecognitionAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut("entries")]
    public async Task<IActionResult> SaveRecognition(
        Guid exchangeGuid,
        [FromBody] SaveRecognitionRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.SaveRecognitionAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(
        Guid exchangeGuid,
        [FromBody] UpdateRecognitionStatusRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.UpdateRecognitionStatusAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("entries/{entryId:int}/recognized")]
    public async Task<IActionResult> SetEntryRecognized(
        Guid exchangeGuid,
        int entryId,
        [FromBody] SetEntryRecognizedRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.SetEntryRecognizedAsync(exchangeGuid, entryId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }
}
