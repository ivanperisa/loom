using ExchangeMapper.Application.DTOs.Exchange;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMapper.API.Controllers;

[Route("exchange")]
[Authorize]
public class ExchangeController(
    IExchangeService exchangeService,
    IInstitutionService institutionService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateExchange([FromBody] CreateExchangeRequest dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.CreateExchangeAsync(userId.Value, dto, ct);
        return Match(result, exchange => CreatedAtAction(nameof(GetMyExchange), exchange));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyExchange(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.GetMyExchangeAsync(userId.Value, ct);
        return Match(result, Ok);
    }

    [HttpDelete("{exchangeId:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid exchangeId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.DeleteExchangeAsync(userId.Value, exchangeId, ct);
        return Match(result, _ => NoContent());
    }

    [HttpPost("{exchangeId:guid}/courses")]
    public async Task<IActionResult> AddCourse(Guid exchangeId, [FromBody] UpsertExchangeCourseRequest dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.AddCourseAsync(userId.Value, exchangeId, dto, ct);
        return Match(result, Ok);
    }

    [HttpPut("{exchangeId:guid}/courses/{courseId:guid}")]
    public async Task<IActionResult> UpdateCourse(Guid exchangeId, Guid courseId, [FromBody] UpsertExchangeCourseRequest dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.UpdateCourseAsync(userId.Value, exchangeId, courseId, dto, ct);
        return Match(result, Ok);
    }

    [HttpDelete("{exchangeId:guid}/courses/{courseId:guid}")]
    public async Task<IActionResult> RemoveCourse(Guid exchangeId, Guid courseId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.RemoveCourseAsync(userId.Value, exchangeId, courseId, ct);
        return Match(result, _ => NoContent());
    }

    [HttpPut("{exchangeId:guid}/courses/{courseId:guid}/grades")]
    public async Task<IActionResult> UpdateGrades(Guid exchangeId, Guid courseId, [FromBody] UpdateGradesRequest dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.UpdateGradesAsync(userId.Value, exchangeId, courseId, dto, ct);
        return Match(result, Ok);
    }

    [HttpPost("{exchangeId:guid}/courses/{courseId:guid}/mappings")]
    public async Task<IActionResult> ProposeMapping(Guid exchangeId, Guid courseId, [FromBody] ProposeMappingRequest dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.ProposeMappingAsync(userId.Value, exchangeId, courseId, dto, ct);
        return Match(result, Ok);
    }

    [HttpPut("{exchangeId:guid}/courses/{courseId:guid}/mappings/{mappingId:guid}")]
    [Authorize(Roles = Roles.Coordinator)]
    public async Task<IActionResult> ReviewMapping(Guid exchangeId, Guid courseId, Guid mappingId, [FromBody] ReviewMappingRequest dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.ReviewMappingAsync(userId.Value, exchangeId, courseId, mappingId, dto, ct);
        return Match(result, Ok);
    }

    [HttpDelete("{exchangeId:guid}/courses/{courseId:guid}/mappings/{mappingId:guid}")]
    public async Task<IActionResult> DeleteMapping(Guid exchangeId, Guid courseId, Guid mappingId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.DeleteMappingAsync(userId.Value, exchangeId, courseId, mappingId, ct);
        return Match(result, _ => NoContent());
    }

    [HttpGet("my/history")]
    public async Task<IActionResult> GetMyHistory(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.GetMyHistoryAsync(userId.Value, ct);
        return Match(result, Ok);
    }

    [HttpGet("{exchangeId:guid}/history")]
    [Authorize(Roles = Roles.Coordinator)]
    public async Task<IActionResult> GetExchangeHistory(Guid exchangeId, CancellationToken ct)
    {
        var result = await exchangeService.GetExchangeHistoryAsync(exchangeId, ct);
        return Match(result, Ok);
    }

    [HttpGet("students")]
    [Authorize(Roles = Roles.Coordinator)]
    public async Task<IActionResult> GetStudentsWithExchange(CancellationToken ct)
    {
        var result = await exchangeService.GetStudentsWithExchangeAsync(ct);
        return Match(result, Ok);
    }

    [HttpPost("{exchangeId:guid}/retract")]
    public async Task<IActionResult> Retract(Guid exchangeId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.RetractAsync(userId.Value, exchangeId, ct);
        return Match(result, Ok);
    }

    [HttpPost("{exchangeId:guid}/submit")]
    public async Task<IActionResult> SubmitForReview(Guid exchangeId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await exchangeService.SubmitForReviewAsync(userId.Value, exchangeId, ct);
        return Match(result, Ok);
    }

    [HttpGet("foreign-institutions")]
    public async Task<IActionResult> GetForeignInstitutions(CancellationToken ct)
    {
        var result = await institutionService.GetForeignInstitutionsAsync(ct);
        return Match(result, Ok);
    }

    [HttpGet("available-courses")]
    public async Task<IActionResult> GetAvailableCourses([FromQuery] string? q, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await institutionService.GetAvailableCoursesAsync(userId.Value, q, ct);
        return Match(result, Ok);
    }
}
