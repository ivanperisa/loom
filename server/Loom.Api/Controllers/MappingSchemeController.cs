using Loom.Api.Filters;
using Loom.Application.Common.Security;
using Loom.Application.DTOs.MappingScheme;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

/// <summary>Every action is also reachable through the access link, see <see cref="ExchangeActorAttribute"/>.</summary>
[Route("api/exchanges/{exchangeGuid:guid}/mapping-scheme")]
[Route("api/exchanges/access/{exchangeGuid:guid}/mapping-scheme")]
[AllowAnonymous]
[ExchangeActor]
public class MappingSchemeController(IMappingSchemeService mappingSchemes, ICurrentActor actor) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid exchangeGuid, CancellationToken ct) =>
        Match(await mappingSchemes.GetMappingSchemeAsync(exchangeGuid, actor.UserId, ct), Ok);

    [HttpPut("entries")]
    public async Task<IActionResult> Save(Guid exchangeGuid, [FromBody] SaveMappingSchemeRequest request, CancellationToken ct) =>
        Match(await mappingSchemes.SaveMappingSchemeAsync(exchangeGuid, actor.UserId, request, ct), Ok);
}
