using Loom.Application.DTOs.Institution;
using Loom.Application.Interfaces.Services;
using Loom.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/institutions")]
[Authorize]
public class InstitutionController(IInstitutionService institutionService) : ApiController
{
    #region Lookups

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
    public async Task<IActionResult> GetPartnerInstitutions(CancellationToken ct)
    {
        var result = await institutionService.GetPartnerInstitutionsAsync(ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("partner/{institutionId:int}/courses")]
    public async Task<IActionResult> GetPartnerCoursesByInstitution(int institutionId, CancellationToken ct)
    {
        var result = await institutionService.GetPartnerCoursesByInstitutionAsync(institutionId, ct);
        return Match(result, Ok);
    }

    #endregion

    #region Partner institutions management

    [HttpPost("partner")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreatePartnerInstitution([FromBody] CreatePartnerInstitutionRequest request, CancellationToken ct)
    {
        var result = await institutionService.CreatePartnerInstitutionAsync(request, ct);
        return Match(result, value => Created($"/api/institutions/partner", value));
    }

    [HttpDelete("partner/{institutionId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeletePartnerInstitution(int institutionId, CancellationToken ct)
    {
        var result = await institutionService.DeletePartnerInstitutionAsync(institutionId, ct);
        return Match(result, _ => NoContent());
    }

    #endregion

    #region Partner courses management

    [AllowAnonymous]
    [HttpPost("partner/{institutionId:int}/courses")]
    public async Task<IActionResult> CreatePartnerCourseByInstitution(int institutionId, [FromBody] CreatePartnerCourseRequest request, CancellationToken ct)
    {
        var result = await institutionService.CreatePartnerCourseByInstitutionAsync(institutionId, request, ct);
        return Match(result, value => Created($"/api/institutions/partner/{institutionId}/courses", value));
    }

    [HttpPut("partner/courses/{courseId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdatePartnerCourse(int courseId, [FromBody] UpdatePartnerCourseRequest request, CancellationToken ct)
    {
        var result = await institutionService.UpdatePartnerCourseAsync(courseId, request, ct);
        return Match(result, Ok);
    }

    [HttpDelete("partner/courses/{courseId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeletePartnerCourse(int courseId, CancellationToken ct)
    {
        var result = await institutionService.DeletePartnerCourseAsync(courseId, ct);
        return Match(result, _ => NoContent());
    }

    [HttpPost("partner/courses/merge")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> MergePartnerCourses([FromBody] MergePartnerCoursesRequest request, CancellationToken ct)
    {
        var result = await institutionService.MergePartnerCoursesAsync(request, ct);
        return Match(result, Ok);
    }

    #endregion
}
