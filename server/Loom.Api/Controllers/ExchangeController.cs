using Loom.Api.Filters;
using Loom.Application.Common.Querying;
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
    public async Task<ActionResult<ExchangeResponse>> GetExchange(Guid exchangeGuid, CancellationToken ct) =>
        Match(await exchanges.GetAsync(exchangeGuid, ct), Ok);

    [HttpGet("mine")]
    public async Task<ActionResult<List<ExchangeSummaryResponse>>> GetMyExchanges(CancellationToken ct) =>
        Ok(await exchanges.ListMineAsync(ct));

    [HttpPost]
    [ProducesResponseType<ExchangeResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ExchangeResponse>> CreateExchange([FromBody] CreateExchangeRequest request, CancellationToken ct) =>
        Match(await exchanges.CreateAsync(request, ct),
            value => CreatedAtAction(nameof(GetExchange), new { exchangeGuid = value.Guid }, value));

    [AllowGuest]
    [HttpPut("{exchangeGuid:guid}")]
    public async Task<ActionResult<ExchangeResponse>> UpdateExchange(Guid exchangeGuid, [FromBody] UpdateExchangeRequest request, CancellationToken ct) =>
        Match(await exchanges.UpdateAsync(exchangeGuid, request, ct), Ok);

    [HttpDelete("{exchangeGuid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteExchange(Guid exchangeGuid, CancellationToken ct) =>
        Match(await exchanges.DeleteAsync(exchangeGuid, ct), _ => NoContent());

    [HttpPatch("{exchangeGuid:guid}/coordinator-message")]
    public async Task<ActionResult<ExchangeResponse>> UpdateCoordinatorMessage(Guid exchangeGuid, [FromBody] UpdateCoordinatorMessageRequest request, CancellationToken ct) =>
        Match(await exchanges.UpdateCoordinatorMessageAsync(exchangeGuid, request.Message, ct), Ok);

    // ---- access link (placeholder students only, managed by the coordinator)

    /// <summary>The live link, created on first use. POST because it may create one.</summary>
    [HttpPost("{exchangeGuid:guid}/access-link")]
    public async Task<ActionResult<AccessLinkResponse>> GetAccessLink(Guid exchangeGuid, CancellationToken ct) =>
        Match(await accessLinks.GetOrCreateAsync(exchangeGuid, ct), Ok);

    [HttpPost("{exchangeGuid:guid}/access-link/regenerate")]
    public async Task<ActionResult<AccessLinkResponse>> RegenerateAccessLink(Guid exchangeGuid, CancellationToken ct) =>
        Match(await accessLinks.RegenerateAsync(exchangeGuid, ct), Ok);

    // ---- partner courses of this exchange's institution (the caller never picks the institution)

    [AllowGuest]
    [HttpGet("{exchangeGuid:guid}/partner-courses")]
    public async Task<ActionResult<PagedResponse<PartnerCourseResponse>>> GetPartnerCourses(Guid exchangeGuid, [FromQuery] PartnerCourseListQuery query, CancellationToken ct) =>
        Match(await partnerCourses.ListForExchangeAsync(exchangeGuid, query, ct), Ok);

    [AllowGuest]
    [HttpPost("{exchangeGuid:guid}/partner-courses")]
    [ProducesResponseType<PartnerCourseResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PartnerCourseResponse>> CreatePartnerCourse(Guid exchangeGuid, [FromBody] PartnerCourseRequest request, CancellationToken ct) =>
        Match(await partnerCourses.CreateForExchangeAsync(exchangeGuid, request, ct), value => StatusCode(StatusCodes.Status201Created, value));
}
