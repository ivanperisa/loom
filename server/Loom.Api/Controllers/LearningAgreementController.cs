using System.Text.Json;
using Loom.Api.Filters;
using Loom.Application.Common.Security;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Helpers;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>Every action is also reachable through the access link (<c>/api/exchanges/access/{guid}/…</c>), see <see cref="ExchangeActorAttribute"/>.</summary>
[Route("api/exchanges/{exchangeGuid:guid}/learning-agreement")]
[Route("api/exchanges/access/{exchangeGuid:guid}/learning-agreement")]
[AllowAnonymous]
[ExchangeActor]
public class LearningAgreementController(ILearningAgreementService learningAgreements, ICurrentActor actor) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid exchangeGuid, CancellationToken ct) =>
        Match(await learningAgreements.GetLearningAgreementAsync(exchangeGuid, actor.UserId, ct), Ok);

    [HttpPut]
    public async Task<IActionResult> Save(Guid exchangeGuid, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct) =>
        Match(await learningAgreements.SaveLearningAgreementAsync(exchangeGuid, actor.UserId, request, ct), Ok);

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeGuid, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct) =>
        Match(await learningAgreements.UpdateLearningAgreementStatusAsync(exchangeGuid, actor.UserId, request, ct), Ok);

    [HttpPatch("message")]
    public async Task<IActionResult> UpdateMessage(Guid exchangeGuid, [FromBody] UpdateLaMessageRequest request, CancellationToken ct) =>
        Match(await learningAgreements.UpdateLearningAgreementMessageAsync(exchangeGuid, actor.UserId, request.Message, ct), Ok);

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(Guid exchangeGuid, CancellationToken ct) =>
        Match(await learningAgreements.GetLearningAgreementHistoryAsync(exchangeGuid, actor.UserId, ct), Ok);

    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshots(Guid exchangeGuid, CancellationToken ct) =>
        Match(await learningAgreements.GetSnapshotsAsync(exchangeGuid, actor.UserId, ct), Ok);

    [HttpPost("snapshots/{snapshotId:int}/restore")]
    public async Task<IActionResult> RestoreSnapshot(Guid exchangeGuid, int snapshotId, CancellationToken ct) =>
        Match(await learningAgreements.RestoreSnapshotAsync(exchangeGuid, snapshotId, actor.UserId, ct), _ => NoContent());

    [HttpGet("export")]
    public async Task<IActionResult> Export(Guid exchangeGuid, CancellationToken ct) =>
        Match(await learningAgreements.ExportMappingsAsync(exchangeGuid, actor.UserId, ct), export => File(
            JsonSerializer.SerializeToUtf8Bytes(export, JsonHelper.DefaultOptions),
            "application/json",
            $"la-export-{DateTime.UtcNow:yyyy-MM-dd}.json"));

    [HttpPost("import")]
    public async Task<IActionResult> Import(Guid exchangeGuid, [FromBody] MappingExportDto dto, CancellationToken ct) =>
        Match(await learningAgreements.ImportMappingsAsync(exchangeGuid, actor.UserId, dto, ct), Ok);
}
