using Loom.Application.Features.Catalog;
using Loom.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/institutions")]
[Authorize]
public class CatalogController(
    HomeCatalogService homeCatalog,
    PartnerInstitutionService partnerInstitutions,
    PartnerCourseService partnerCourses,
    CourseMergeService courseMerge) : ApiController
{
    [HttpGet("home")]
    public async Task<IActionResult> GetHomeInstitutions(CancellationToken ct) =>
        Ok(await homeCatalog.GetHomeInstitutionsAsync(ct));

    [HttpGet("home-programs")]
    public async Task<IActionResult> GetHomePrograms(CancellationToken ct) =>
        Ok(await homeCatalog.GetHomeProgramsAsync(ct));

    // ---- partner institutions

    [HttpGet("partner")]
    public async Task<IActionResult> GetPartnerInstitutions([FromQuery] PartnerInstitutionListQuery query, CancellationToken ct) =>
        Ok(await partnerInstitutions.ListAsync(query, ct));

    [HttpPost("partner")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreatePartnerInstitution([FromBody] PartnerInstitutionRequest request, CancellationToken ct) =>
        Match(await partnerInstitutions.CreateAsync(request, ct), value => Created("/api/institutions/partner", value));

    [HttpPut("partner/{institutionId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdatePartnerInstitution(int institutionId, [FromBody] PartnerInstitutionRequest request, CancellationToken ct) =>
        Match(await partnerInstitutions.UpdateAsync(institutionId, request, ct), Ok);

    [HttpDelete("partner/{institutionId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeletePartnerInstitution(int institutionId, CancellationToken ct) =>
        Match(await partnerInstitutions.DeleteAsync(institutionId, ct), _ => NoContent());

    [HttpPatch("partner/{institutionId:int}/restore")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> RestorePartnerInstitution(int institutionId, CancellationToken ct) =>
        Match(await partnerInstitutions.RestoreAsync(institutionId, ct), _ => NoContent());

    // ---- partner courses

    [HttpGet("partner/{institutionId:int}/courses")]
    public async Task<IActionResult> GetPartnerCourses(int institutionId, [FromQuery] PartnerCourseListQuery query, CancellationToken ct) =>
        Ok(await partnerCourses.ListAsync(institutionId, query, ct));

    /// <summary>Admin catalogue. Students and guests add courses through <c>POST /api/exchanges/{guid}/partner-courses</c>.</summary>
    [HttpPost("partner/{institutionId:int}/courses")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreatePartnerCourse(int institutionId, [FromBody] PartnerCourseRequest request, CancellationToken ct) =>
        Match(await partnerCourses.CreateAsync(institutionId, request, ct),
            value => Created($"/api/institutions/partner/{institutionId}/courses", value));

    [HttpPut("partner/courses/{courseId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdatePartnerCourse(int courseId, [FromBody] PartnerCourseRequest request, CancellationToken ct) =>
        Match(await partnerCourses.UpdateAsync(courseId, request, ct), Ok);

    [HttpDelete("partner/courses/{courseId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeletePartnerCourse(int courseId, CancellationToken ct) =>
        Match(await partnerCourses.DeleteAsync(courseId, ct), _ => NoContent());

    [HttpPatch("partner/courses/{courseId:int}/restore")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> RestorePartnerCourse(int courseId, CancellationToken ct) =>
        Match(await partnerCourses.RestoreAsync(courseId, ct), _ => NoContent());

    [HttpPost("partner/courses/merge")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> MergePartnerCourses([FromBody] MergePartnerCoursesRequest request, CancellationToken ct) =>
        Match(await courseMerge.MergeAsync(request, ct), Ok);

    [HttpGet("partner/courses/{courseId:int}/usage")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetPartnerCourseUsage(int courseId, CancellationToken ct) =>
        Match(await partnerCourses.GetUsageAsync(courseId, ct), Ok);
}
