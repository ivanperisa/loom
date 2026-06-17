using System.Text.Json;
using Loom.Api.Extensions;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Helpers;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges/{exchangeGuid:guid}/learning-agreement")]
[Authorize]
public class LearningAgreementController(ILearningAgreementService learningAgreementService, IExchangeService exchangeService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetLearningAgreement(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await learningAgreementService.GetLearningAgreementAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut]
    public async Task<IActionResult> SaveLearningAgreement(Guid exchangeGuid, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var result = await learningAgreementService.SaveLearningAgreementAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeGuid, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        var result = await learningAgreementService.UpdateLearningAgreementStatusAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement")]
    public async Task<IActionResult> GetPublicLearningAgreement(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.GetLearningAgreementAsync(exchangeGuid, studentIdResult.Value, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPut("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement")]
    public async Task<IActionResult> SavePublicLearningAgreement(Guid exchangeGuid, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.SaveLearningAgreementAsync(exchangeGuid, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPatch("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement/status")]
    public async Task<IActionResult> UpdatePublicStatus(Guid exchangeGuid, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.UpdateLearningAgreementStatusAsync(exchangeGuid, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("message")]
    public async Task<IActionResult> UpdateLearningAgreementMessage(Guid exchangeGuid, [FromBody] UpdateLaMessageRequest request, CancellationToken ct)
    {
        var result = await learningAgreementService.UpdateLearningAgreementMessageAsync(exchangeGuid, GetCurrentUserId(), request.Message, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPatch("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement/message")]
    public async Task<IActionResult> UpdatePublicLearningAgreementMessage(Guid exchangeGuid, [FromBody] UpdateLaMessageRequest request, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.UpdateLearningAgreementMessageAsync(exchangeGuid, studentIdResult.Value, request.Message, ct);
        return Match(result, Ok);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetLearningAgreementHistory(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await learningAgreementService.GetLearningAgreementHistoryAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement/history")]
    public async Task<IActionResult> GetPublicLearningAgreementHistory(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.GetLearningAgreementHistoryAsync(exchangeGuid, studentIdResult.Value, ct);
        return Match(result, Ok);
    }

    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshots(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await learningAgreementService.GetSnapshotsAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement/snapshots")]
    public async Task<IActionResult> GetPublicSnapshots(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.GetSnapshotsAsync(exchangeGuid, studentIdResult.Value, ct);
        return Match(result, Ok);
    }

    [HttpPost("snapshots/{snapshotId:int}/restore")]
    public async Task<IActionResult> RestoreSnapshot(Guid exchangeGuid, int snapshotId, CancellationToken ct)
    {
        var result = await learningAgreementService.RestoreSnapshotAsync(exchangeGuid, snapshotId, GetCurrentUserId(), ct);
        return Match(result, _ => NoContent());
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportMappings(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await learningAgreementService.ExportMappingsAsync(exchangeGuid, GetCurrentUserId(), ct);
        if (result.IsError) return result.Errors.ToProblemDetails(this);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(result.Value, JsonHelper.DefaultOptions);
        var fileName = $"la-export-{DateTime.UtcNow:yyyy-MM-dd}.json";
        return File(bytes, "application/json", fileName);
    }

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement/export")]
    public async Task<IActionResult> ExportPublicMappings(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.ExportMappingsAsync(exchangeGuid, studentIdResult.Value, ct);
        if (result.IsError) return result.Errors.ToProblemDetails(this);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(result.Value, JsonHelper.DefaultOptions);
        var fileName = $"la-export-{DateTime.UtcNow:yyyy-MM-dd}.json";
        return File(bytes, "application/json", fileName);
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportMappings(Guid exchangeGuid, [FromBody] MappingExportDto dto, CancellationToken ct)
    {
        var result = await learningAgreementService.ImportMappingsAsync(exchangeGuid, GetCurrentUserId(), dto, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPost("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement/import")]
    public async Task<IActionResult> ImportPublicMappings(Guid exchangeGuid, [FromBody] MappingExportDto dto, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.ImportMappingsAsync(exchangeGuid, studentIdResult.Value, dto, ct);
        return Match(result, Ok);
    }
}
