using Loom.Api.Filters;
using Loom.Application.Features.Completion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>Guests (access link) can use every action, for their own exchange.</summary>
[Route("api/exchanges/{exchangeGuid:guid}/mapping-scheme")]
[AllowGuest]
[ExchangeActor]
public class MappingSchemeController(MappingSchemeService mappingSchemes) : ApiController
{
    [HttpGet]
    public async Task<ActionResult<MappingSchemeResponse>> Get(Guid exchangeGuid, CancellationToken ct) =>
        Match(await mappingSchemes.GetAsync(exchangeGuid, ct), Ok);

    [HttpPut("entries")]
    public async Task<ActionResult<MappingSchemeResponse>> Save(Guid exchangeGuid, [FromBody] SaveMappingSchemeRequest request, CancellationToken ct) =>
        Match(await mappingSchemes.SaveAsync(exchangeGuid, request, ct), Ok);
}
