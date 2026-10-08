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
    public async Task<IActionResult> Get(Guid exchangeGuid, CancellationToken ct) =>
        Match(await mappingSchemes.GetMappingSchemeAsync(exchangeGuid, ct), Ok);

    [HttpPut("entries")]
    public async Task<IActionResult> Save(Guid exchangeGuid, [FromBody] SaveMappingSchemeRequest request, CancellationToken ct) =>
        Match(await mappingSchemes.SaveMappingSchemeAsync(exchangeGuid, request, ct), Ok);
}
