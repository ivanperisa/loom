using Loom.Api.Filters;
using Loom.Application.Features.Catalog;
using Loom.Application.Features.Exchanges;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges")]
[Authorize]
[ExchangeActor]
public class ExchangeController(ExchangeService exchanges, AccessLinkService accessLinks, PartnerCourseService partnerCourses) : ApiController
{
    [AllowGuest]
    [HttpGet("{exchangeGuid:guid}")]
    public async Task<IActionResult> GetExchange(Guid exchangeGuid, CancellationToken ct) =>
        Match(await exchanges.GetAsync(exchangeGuid, ct), Ok);

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyExchanges(CancellationToken ct) =>
        Ok(await exchanges.ListMineAsync(ct));

    [HttpPost]
    public async Task<IActionResult> CreateExchange([FromBody] CreateExchangeRequest request, CancellationToken ct) =>
        Match(await exchanges.CreateAsync(request, ct),
            value => CreatedAtAction(nameof(GetExchange), new { exchangeGuid = value.Guid }, value));

    [AllowGuest]
    [HttpPut("{exchangeGuid:guid}")]
    public async Task<IActionResult> UpdateExchange(Guid exchangeGuid, [FromBody] UpdateExchangeRequest request, CancellationToken ct) =>
        Match(await exchanges.UpdateAsync(exchangeGuid, request, ct), Ok);

    [HttpDelete("{exchangeGuid:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid exchangeGuid, CancellationToken ct) =>
        Match(await exchanges.DeleteAsync(exchangeGuid, ct), _ => NoContent());

    [HttpPut("{exchangeGuid:guid}/coordinator-message")]
    public async Task<IActionResult> UpdateCoordinatorMessage(Guid exchangeGuid, [FromBody] UpdateCoordinatorMessageRequest request, CancellationToken ct) =>
        Match(await exchanges.UpdateCoordinatorMessageAsync(exchangeGuid, request.Message, ct), Ok);

    // ---- access link (placeholder students only, managed by the coordinator)

    /// <summary>The live link, created on first use. POST because it may create one.</summary>
    [HttpPost("{exchangeGuid:guid}/access-link")]
    public async Task<IActionResult> GetAccessLink(Guid exchangeGuid, CancellationToken ct) =>
        Match(await accessLinks.GetOrCreateAsync(exchangeGuid, ct), Ok);

    [HttpPost("{exchangeGuid:guid}/access-link/regenerate")]
    public async Task<IActionResult> RegenerateAccessLink(Guid exchangeGuid, CancellationToken ct) =>
        Match(await accessLinks.RegenerateAsync(exchangeGuid, ct), Ok);

    // ---- partner courses of this exchange's institution (the caller never picks the institution)

    [AllowGuest]
    [HttpGet("{exchangeGuid:guid}/partner-courses")]
    public async Task<IActionResult> GetPartnerCourses(Guid exchangeGuid, [FromQuery] PartnerCourseListQuery query, CancellationToken ct) =>
        Match(await partnerCourses.ListForExchangeAsync(exchangeGuid, query, ct), Ok);

    [AllowGuest]
    [HttpPost("{exchangeGuid:guid}/partner-courses")]
    public async Task<IActionResult> CreatePartnerCourse(Guid exchangeGuid, [FromBody] PartnerCourseRequest request, CancellationToken ct) =>
        Match(await partnerCourses.CreateForExchangeAsync(exchangeGuid, request, ct), value => StatusCode(StatusCodes.Status201Created, value));
}
