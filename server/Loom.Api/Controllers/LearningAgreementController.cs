using System.Text.Json;
using Loom.Api.Filters;
using Loom.Application.Common;
using Loom.Application.Features.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>Every action is also reachable through the access link (<c>/api/exchanges/access/{guid}/…</c>), see <see cref="ExchangeActorAttribute"/>.</summary>
[Route("api/exchanges/{exchangeGuid:guid}/learning-agreement")]
[Route("api/exchanges/access/{exchangeGuid:guid}/learning-agreement")]
[AllowAnonymous]
[ExchangeActor]
public class LearningAgreementController(
    LearningAgreementService learningAgreements,
    LearningAgreementWorkflow workflow,
    LaVersionService versions,
    LaTransferService transfer) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid exchangeGuid, CancellationToken ct) =>
        Match(await learningAgreements.GetAsync(exchangeGuid, ct), Ok);

    [HttpPut]
    public async Task<IActionResult> Save(Guid exchangeGuid, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct) =>
        Match(await learningAgreements.SaveAsync(exchangeGuid, request, ct), Ok);

    [HttpPatch("message")]
    public async Task<IActionResult> UpdateMessage(Guid exchangeGuid, [FromBody] UpdateLaMessageRequest request, CancellationToken ct) =>
        Match(await learningAgreements.UpdateMessageAsync(exchangeGuid, request.Message, ct), Ok);

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeGuid, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct) =>
        Match(await workflow.SetStatusAsync(exchangeGuid, request, ct), Ok);

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(Guid exchangeGuid, CancellationToken ct) =>
        Match(await versions.GetApprovalHistoryAsync(exchangeGuid, ct), Ok);

    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshots(Guid exchangeGuid, CancellationToken ct) =>
        Match(await versions.ListSnapshotsAsync(exchangeGuid, ct), Ok);

    [HttpPost("snapshots/{snapshotId:int}/restore")]
    public async Task<IActionResult> RestoreSnapshot(Guid exchangeGuid, int snapshotId, CancellationToken ct) =>
        Match(await versions.RestoreAsync(exchangeGuid, snapshotId, ct), _ => NoContent());

    [HttpGet("export")]
    public async Task<IActionResult> Export(Guid exchangeGuid, CancellationToken ct) =>
        Match(await transfer.ExportAsync(exchangeGuid, ct), export => File(
            JsonSerializer.SerializeToUtf8Bytes(export, JsonHelper.DefaultOptions),
            "application/json",
            $"la-export-{DateTime.UtcNow:yyyy-MM-dd}.json"));

    [HttpPost("import")]
    public async Task<IActionResult> Import(Guid exchangeGuid, [FromBody] MappingExportDto file, CancellationToken ct) =>
        Match(await transfer.ImportAsync(exchangeGuid, file, ct), Ok);
}
