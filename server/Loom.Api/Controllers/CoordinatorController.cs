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
    public async Task<IActionResult> GetCoordinators(CancellationToken ct) =>
        Ok(await directory.ListAsync(ct));

    [HttpGet("students")]
    public async Task<IActionResult> GetMyStudents([FromQuery] StudentListQuery query, CancellationToken ct) =>
        Match(await students.ListMineAsync(query, ct), Ok);

    [HttpPost("students")]
    public async Task<IActionResult> CreatePlaceholderStudent([FromBody] PlaceholderStudentRequest request, CancellationToken ct) =>
        Match(await students.CreatePlaceholderAsync(request, ct), value => CreatedAtAction(nameof(GetMyStudents), value));

    [HttpPut("students/{studentId:int}")]
    public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] PlaceholderStudentRequest request, CancellationToken ct) =>
        Match(await students.UpdatePlaceholderAsync(studentId, request, ct), Ok);

    [HttpDelete("students/{studentId:int}")]
    public async Task<IActionResult> DeleteStudent(int studentId, CancellationToken ct) =>
        Match(await students.DeletePlaceholderAsync(studentId, ct), _ => NoContent());

    [HttpGet("students/filters")]
    public async Task<IActionResult> GetStudentFilters(CancellationToken ct) =>
        Match(await students.FiltersAsync(ct), Ok);
}
