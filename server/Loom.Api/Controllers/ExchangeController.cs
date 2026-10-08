using Loom.Api.Filters;
using Loom.Application.Features.Exchanges;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges")]
[Authorize]
[ExchangeActor]
public class ExchangeController(ExchangeService exchanges) : ApiController
{
    [AllowAnonymous]
    [HttpGet("{exchangeGuid:guid}")]
    [HttpGet("access/{exchangeGuid:guid}")]
    public async Task<IActionResult> GetExchange(Guid exchangeGuid, CancellationToken ct) =>
        Match(await exchanges.GetAsync(exchangeGuid, ct), Ok);

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyExchanges(CancellationToken ct) =>
        Ok(await exchanges.ListMineAsync(ct));

    [HttpPost]
    public async Task<IActionResult> CreateExchange([FromBody] CreateExchangeRequest request, CancellationToken ct) =>
        Match(await exchanges.CreateAsync(request, ct),
            value => CreatedAtAction(nameof(GetExchange), new { exchangeGuid = value.Guid }, value));

    [AllowAnonymous]
    [HttpPut("{exchangeGuid:guid}")]
    [HttpPut("access/{exchangeGuid:guid}")]
    public async Task<IActionResult> UpdateExchange(Guid exchangeGuid, [FromBody] UpdateExchangeRequest request, CancellationToken ct) =>
        Match(await exchanges.UpdateAsync(exchangeGuid, request, ct), Ok);

    [HttpDelete("{exchangeGuid:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid exchangeGuid, CancellationToken ct) =>
        Match(await exchanges.DeleteAsync(exchangeGuid, ct), _ => NoContent());

    [HttpPut("{exchangeGuid:guid}/coordinator-message")]
    public async Task<IActionResult> UpdateCoordinatorMessage(Guid exchangeGuid, [FromBody] UpdateCoordinatorMessageRequest request, CancellationToken ct) =>
        Match(await exchanges.UpdateCoordinatorMessageAsync(exchangeGuid, request.Message, ct), Ok);

    [HttpPost("{exchangeGuid:guid}/regenerate-access-link")]
    public async Task<IActionResult> RegenerateAccessLink(Guid exchangeGuid, CancellationToken ct) =>
        Match(await exchanges.RegenerateAccessLinkAsync(exchangeGuid, ct), guid => Ok(new { guid }));
}
