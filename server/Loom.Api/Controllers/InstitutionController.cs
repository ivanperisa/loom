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

    [HttpGet("foreign-programs")]
    public async Task<IActionResult> GetForeignPrograms(CancellationToken ct)
    {
        var result = await institutionService.GetForeignProgramsAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("study-programs")]
    public async Task<IActionResult> GetStudyPrograms(CancellationToken ct)
    {
        var result = await institutionService.GetStudyProgramsAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("foreign-programs/{foreignProgramId:guid}/courses")]
    public async Task<IActionResult> GetForeignCourses(Guid foreignProgramId, CancellationToken ct)
    {
        var result = await institutionService.GetForeignCoursesAsync(foreignProgramId, ct);
        return Match(result, Ok);
    }

    [HttpGet("coordinators")]
    public async Task<IActionResult> GetCoordinators(CancellationToken ct)
    {
        var result = await institutionService.GetCoordinatorsAsync(ct);
        return Match(result, Ok);
    }
}
