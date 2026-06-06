using Loom.Application.DTOs.Institution;
using Loom.Application.Interfaces.Services;
using Loom.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/admin/institutions")]
[Authorize(Roles = Roles.Admin)]
public class AdminInstitutionController(IInstitutionService institutionService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetPartnerInstitutions(CancellationToken ct)
    {
        var result = await institutionService.GetPartnerInstitutionsAdminAsync(ct);
        return Match(result, Ok);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePartnerInstitution([FromBody] CreatePartnerInstitutionRequest request, CancellationToken ct)
    {
        var result = await institutionService.CreatePartnerInstitutionAsync(request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetPartnerInstitutions), value));
    }

    [HttpDelete("{institutionId:int}")]
    public async Task<IActionResult> DeletePartnerInstitution(int institutionId, CancellationToken ct)
    {
        var result = await institutionService.DeletePartnerInstitutionAsync(institutionId, ct);
        return Match(result, _ => NoContent());
    }

    [HttpPost("{institutionId:int}/programs")]
    public async Task<IActionResult> CreatePartnerProgram(int institutionId, [FromBody] CreatePartnerProgramRequest request, CancellationToken ct)
    {
        var result = await institutionService.CreatePartnerProgramAsync(institutionId, request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetPartnerInstitutions), value));
    }

    [HttpDelete("programs/{programId:int}")]
    public async Task<IActionResult> DeletePartnerProgram(int programId, CancellationToken ct)
    {
        var result = await institutionService.DeletePartnerProgramAsync(programId, ct);
        return Match(result, _ => NoContent());
    }

    [HttpPost("programs/{programId:int}/courses")]
    public async Task<IActionResult> CreatePartnerCourse(int programId, [FromBody] CreatePartnerCourseRequest request, CancellationToken ct)
    {
        var result = await institutionService.CreatePartnerCourseAsync(programId, request, ct);
        return Match(result, value => CreatedAtAction(nameof(GetPartnerInstitutions), value));
    }

    [HttpDelete("programs/courses/{courseId:int}")]
    public async Task<IActionResult> DeletePartnerCourse(int courseId, CancellationToken ct)
    {
        var result = await institutionService.DeletePartnerCourseAsync(courseId, ct);
        return Match(result, _ => NoContent());
    }
}
