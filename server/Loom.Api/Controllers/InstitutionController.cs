using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/institutions")]
[Authorize]
public class InstitutionController(IInstitutionService institutionService) : ApiController
{
    [HttpGet("home")]
    public async Task<IActionResult> GetHomeInstitutions(CancellationToken ct)
    {
        var result = await institutionService.GetHomeInstitutionsAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("home-programs")]
    public async Task<IActionResult> GetHomePrograms(CancellationToken ct)
    {
        var result = await institutionService.GetHomeProgramsAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("partner")]
    public async Task<IActionResult> GetPartnerInstitutionsAdmin(CancellationToken ct)
    {
        var result = await institutionService.GetPartnerInstitutionsAdminAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("partner-programs")]
    public async Task<IActionResult> GetPartnerPrograms(CancellationToken ct)
    {
        var result = await institutionService.GetPartnerProgramsAsync(ct);
        return Match(result, Ok);
    }

    
    [HttpGet("partner-programs/{partnerProgramId:int}/courses")]
    public async Task<IActionResult> GetPartnerCourses(int partnerProgramId, CancellationToken ct)
    {
        var result = await institutionService.GetPartnerCoursesAsync(partnerProgramId, ct);
        return Match(result, Ok);
    }
}
