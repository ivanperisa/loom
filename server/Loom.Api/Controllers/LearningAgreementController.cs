using System.Text.Json;
using Loom.Api.Filters;
using Loom.Application.Common;
using Loom.Application.Features.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>Guests (access link) can use every action, for their own exchange.</summary>
[Route("api/exchanges/{exchangeGuid:guid}/learning-agreement")]
[AllowGuest]
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

    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions(Guid exchangeGuid, CancellationToken ct) =>
        Match(await versions.ListAsync(exchangeGuid, ct), Ok);

    /// <summary>Loads a version (approval or backup) into the draft; the status never changes.</summary>
    [HttpPost("versions/{versionId:int}/restore")]
    public async Task<IActionResult> Restore(Guid exchangeGuid, int versionId, CancellationToken ct) =>
        Match(await versions.RestoreAsync(exchangeGuid, versionId, ct), Ok);

    /// <summary>"Export for import": JSON that another exchange (or this one) can import.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(Guid exchangeGuid, CancellationToken ct) =>
        Match(await transfer.ExportAsync(exchangeGuid, ct), export => File(
            JsonSerializer.SerializeToUtf8Bytes(export, JsonHelper.DefaultOptions),
            "application/json",
            $"la-export-{DateTime.UtcNow:yyyy-MM-dd}.json"));

    /// <summary>What importing the file would change. Nothing is saved.</summary>
    [HttpPost("import/preview")]
    public async Task<IActionResult> PreviewImport(Guid exchangeGuid, [FromBody] MappingExportDto file, CancellationToken ct) =>
        Match(await transfer.PreviewAsync(exchangeGuid, file, ct), Ok);

    /// <summary>Replaces the draft with the file (after the same checks as the preview), keeping a backup.</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(Guid exchangeGuid, [FromBody] MappingExportDto file, CancellationToken ct) =>
        Match(await transfer.ApplyAsync(exchangeGuid, file, ct), Ok);
}
