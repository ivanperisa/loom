using Loom.Api.Filters;
using Loom.Application.Features.Completion;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>Guests (access link) can use every action, for their own exchange; approving is the coordinator's.</summary>
[Route("api/exchanges/{exchangeGuid:guid}/recognition")]
[AllowGuest]
[ExchangeActor]
public class RecognitionController(RecognitionService recognitions) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid exchangeGuid, CancellationToken ct) =>
        Match(await recognitions.GetAsync(exchangeGuid, ct), Ok);

    /// <summary>"Start final recognition": freezes the LA and table 1 for good and creates the results.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start(Guid exchangeGuid, CancellationToken ct) =>
        Match(await recognitions.StartAsync(exchangeGuid, ct), Ok);

    /// <summary>Table 2: grades per partner course.</summary>
    [HttpPut("grades")]
    public async Task<IActionResult> SaveGrades(Guid exchangeGuid, [FromBody] SaveGradesRequest request, CancellationToken ct) =>
        Match(await recognitions.SaveGradesAsync(exchangeGuid, request, ct), Ok);

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeGuid, [FromBody] UpdateRecognitionStatusRequest request, CancellationToken ct) =>
        Match(await recognitions.SetStatusAsync(exchangeGuid, request, ct), Ok);

    [HttpPatch("message")]
    public async Task<IActionResult> UpdateMessage(Guid exchangeGuid, [FromBody] UpdateRecognitionMessageRequest request, CancellationToken ct) =>
        Match(await recognitions.UpdateMessageAsync(exchangeGuid, request.Message, ct), Ok);

    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions(Guid exchangeGuid, CancellationToken ct) =>
        Match(await recognitions.ListVersionsAsync(exchangeGuid, ct), Ok);
}
