using Loom.Api.Extensions;
using Loom.Application.DTOs.Recognition;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Loom.Api.Controllers;

[Route("api/exchanges/{exchangeGuid:guid}/recognition")]
[Authorize]
public class RecognitionController(IRecognitionService recognitionService, IExchangeService exchangeService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetOrCreateRecognition(Guid exchangeGuid, CancellationToken ct)
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

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/recognition")]
    public async Task<IActionResult> GetOrCreatePublic(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await recognitionService.GetOrCreateRecognitionAsync(exchangeGuid, studentIdResult.Value, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPut("/api/exchanges/access/{exchangeGuid:guid}/recognition/entries")]
    public async Task<IActionResult> SavePublicRecognition(
        Guid exchangeGuid,
        [FromBody] SaveRecognitionRequest request,
        CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await recognitionService.SaveRecognitionAsync(exchangeGuid, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPatch("/api/exchanges/access/{exchangeGuid:guid}/recognition/status")]
    public async Task<IActionResult> UpdatePublicStatus(
        Guid exchangeGuid,
        [FromBody] UpdateRecognitionStatusRequest request,
        CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await recognitionService.UpdateRecognitionStatusAsync(exchangeGuid, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPatch("/api/exchanges/access/{exchangeGuid:guid}/recognition/entries/{entryId:int}/recognized")]
    public async Task<IActionResult> SetPublicEntryRecognized(
        Guid exchangeGuid,
        int entryId,
        [FromBody] SetEntryRecognizedRequest request,
        CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await recognitionService.SetEntryRecognizedAsync(exchangeGuid, entryId, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("message")]
    public async Task<IActionResult> UpdateRecognitionMessage(
        Guid exchangeGuid,
        [FromBody] UpdateRecognitionMessageRequest request,
        CancellationToken ct)
    {
        var result = await recognitionService.UpdateRecognitionMessageAsync(exchangeGuid, GetCurrentUserId(), request.Message, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPatch("/api/exchanges/access/{exchangeGuid:guid}/recognition/message")]
    public async Task<IActionResult> UpdatePublicRecognitionMessage(
        Guid exchangeGuid,
        [FromBody] UpdateRecognitionMessageRequest request,
        CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await recognitionService.UpdateRecognitionMessageAsync(exchangeGuid, studentIdResult.Value, request.Message, ct);
        return Match(result, Ok);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetRecognitionHistory(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await recognitionService.GetRecognitionHistoryAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/recognition/history")]
    public async Task<IActionResult> GetPublicRecognitionHistory(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await recognitionService.GetRecognitionHistoryAsync(exchangeGuid, studentIdResult.Value, ct);
        return Match(result, Ok);
    }
}
