using ExchangeMapper.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMapper.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("institutions")]
public class InstitutionController(
    IInstitutionService institutionService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetHomeInstitutions(CancellationToken ct)
    {
        var result = await institutionService.GetHomeInstitutionsAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("{id:guid}/programs")]
    public async Task<IActionResult> GetProgramsByInstitution(Guid id, CancellationToken ct)
    {
        var result = await institutionService.GetProgramsByInstitutionAsync(id, ct);
        return Match(result, Ok);
    }

    [HttpGet("{id:guid}/programs/{programId:guid}/profiles")]
    public async Task<IActionResult> GetProfilesByProgram(Guid id, Guid programId, CancellationToken ct)
    {
        var result = await institutionService.GetProfilesByProgramAsync(id, programId, ct);
        return Match(result, Ok);
    }
}
