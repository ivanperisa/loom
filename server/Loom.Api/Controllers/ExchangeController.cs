using Loom.Application.DTOs.Exchange;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges")]
[Authorize]
public class ExchangeController(IExchangeService exchangeService) : ApiController
{
    [HttpGet("{exchangeGuid:guid}")]
    public async Task<IActionResult> GetExchange(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await exchangeService.GetExchangeAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("access/{exchangeGuid:guid}")]
    public async Task<IActionResult> GetPublicExchange(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await exchangeService.GetPublicExchangeAsync(exchangeGuid, ct);
        return Match(result, Ok);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyExchanges(CancellationToken ct)
    {
        var result = await exchangeService.GetMyExchangesAsync(GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExchange([FromBody] CreateExchangeRequest request, CancellationToken ct)
    {
        var result = await exchangeService.CreateExchangeAsync(GetCurrentUserId(), request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetExchange), new { exchangeGuid = value.Guid }, value));
    }

    [HttpDelete("{exchangeGuid:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await exchangeService.DeleteExchangeAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, _ => NoContent());
    }

    [HttpPut("{exchangeGuid:guid}/coordinator-message")]
    public async Task<IActionResult> UpdateCoordinatorMessage(Guid exchangeGuid, [FromBody] UpdateCoordinatorMessageRequest request, CancellationToken ct)
    {
        var result = await exchangeService.UpdateCoordinatorMessageAsync(exchangeGuid, GetCurrentUserId(), request.Message, ct);
        return Match(result, Ok);
    }

    [HttpPatch("{exchangeGuid:guid}/ewp-link")]
    public async Task<IActionResult> UpdateEwpLink(Guid exchangeGuid, [FromBody] UpdateEwpLinkRequest request, CancellationToken ct)
    {
        var result = await exchangeService.UpdateEwpLinkAsync(exchangeGuid, GetCurrentUserId(), request.EwpLink, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPatch("access/{exchangeGuid:guid}/ewp-link")]
    public async Task<IActionResult> UpdateEwpLinkPublic(Guid exchangeGuid, [FromBody] UpdateEwpLinkRequest request, CancellationToken ct)
    {
        var result = await exchangeService.UpdateEwpLinkAsync(exchangeGuid, null, request.EwpLink, ct);
        return Match(result, Ok);
    }
}
