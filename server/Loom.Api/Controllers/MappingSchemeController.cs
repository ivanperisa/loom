using Loom.Api.Extensions;
using Loom.Application.DTOs.MappingScheme;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges/{exchangeGuid:guid}/mapping-scheme")]
[Authorize]
public class MappingSchemeController(IMappingSchemeService mappingSchemeService, IExchangeService exchangeService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMappingScheme(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await mappingSchemeService.GetMappingSchemeAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut("entries")]
    public async Task<IActionResult> SaveMappingScheme(
        Guid exchangeGuid,
        [FromBody] SaveMappingSchemeRequest request,
        CancellationToken ct)
    {
        var result = await mappingSchemeService.SaveMappingSchemeAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/mapping-scheme")]
    public async Task<IActionResult> GetPublicMappingScheme(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await mappingSchemeService.GetMappingSchemeAsync(exchangeGuid, studentIdResult.Value, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPut("/api/exchanges/access/{exchangeGuid:guid}/mapping-scheme/entries")]
    public async Task<IActionResult> SavePublicMappingScheme(
        Guid exchangeGuid,
        [FromBody] SaveMappingSchemeRequest request,
        CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await mappingSchemeService.SaveMappingSchemeAsync(exchangeGuid, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }
}
