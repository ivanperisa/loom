using Loom.Application.Common.Querying;
using Loom.Application.Features.Coordination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/coordinator")]
[Authorize]
public class CoordinatorController(CoordinatorDirectoryService directory, StudentService students) : ApiController
{
    /// <summary>Everyone selectable as coordinator (used by students too).</summary>
    [HttpGet("/api/coordinators")]
    public async Task<ActionResult<List<CoordinatorOptionResponse>>> GetCoordinators(CancellationToken ct) =>
        Ok(await directory.ListAsync(ct));

    [HttpGet("students")]
    public async Task<ActionResult<PagedResponse<CoordinatorStudentResponse>>> GetMyStudents([FromQuery] StudentListQuery query, CancellationToken ct) =>
        Match(await students.ListMineAsync(query, ct), Ok);

    [HttpPost("students")]
    [ProducesResponseType<CoordinatorStudentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CoordinatorStudentResponse>> CreatePlaceholderStudent([FromBody] PlaceholderStudentRequest request, CancellationToken ct) =>
        Match(await students.CreatePlaceholderAsync(request, ct), value => CreatedAtAction(nameof(GetMyStudents), value));

    [HttpPut("students/{studentId:int}")]
    public async Task<ActionResult<CoordinatorStudentResponse>> UpdateStudent(int studentId, [FromBody] PlaceholderStudentRequest request, CancellationToken ct) =>
        Match(await students.UpdatePlaceholderAsync(studentId, request, ct), Ok);

    [HttpDelete("students/{studentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteStudent(int studentId, CancellationToken ct) =>
        Match(await students.DeletePlaceholderAsync(studentId, ct), _ => NoContent());

    [HttpGet("students/filters")]
    public async Task<ActionResult<StudentFiltersResponse>> GetStudentFilters(CancellationToken ct) =>
        Match(await students.FiltersAsync(ct), Ok);
}
