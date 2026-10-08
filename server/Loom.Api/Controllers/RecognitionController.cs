using Loom.Api.Filters;
using Loom.Application.Common.Security;
using Loom.Application.DTOs.Recognition;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>Every action is also reachable through the access link, see <see cref="ExchangeActorAttribute"/>.</summary>
[Route("api/exchanges/{exchangeGuid:guid}/recognition")]
[Route("api/exchanges/access/{exchangeGuid:guid}/recognition")]
[AllowAnonymous]
[ExchangeActor]
public class RecognitionController(IRecognitionService recognitions, ICurrentActor actor) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid exchangeGuid, CancellationToken ct) =>
        Match(await recognitions.GetOrCreateRecognitionAsync(exchangeGuid, actor.UserId, ct), Ok);

    [HttpPut("entries")]
    public async Task<IActionResult> Save(Guid exchangeGuid, [FromBody] SaveRecognitionRequest request, CancellationToken ct) =>
        Match(await recognitions.SaveRecognitionAsync(exchangeGuid, actor.UserId, request, ct), Ok);

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeGuid, [FromBody] UpdateRecognitionStatusRequest request, CancellationToken ct) =>
        Match(await recognitions.UpdateRecognitionStatusAsync(exchangeGuid, actor.UserId, request, ct), Ok);

    [HttpPatch("message")]
    public async Task<IActionResult> UpdateMessage(Guid exchangeGuid, [FromBody] UpdateRecognitionMessageRequest request, CancellationToken ct) =>
        Match(await recognitions.UpdateRecognitionMessageAsync(exchangeGuid, actor.UserId, request.Message, ct), Ok);

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(Guid exchangeGuid, CancellationToken ct) =>
        Match(await recognitions.GetRecognitionHistoryAsync(exchangeGuid, actor.UserId, ct), Ok);
}
