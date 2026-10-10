namespace Loom.Application.Features.Completion;

/// <summary>
/// The whole scheme: every placement to keep (moved or resized), new ones (id ≤ 0) splitting a course already in it,
/// and course statuses to change (a course marked not passed). Grades are saved with table 2, not here.
/// </summary>
public record SaveMappingSchemeRequest(List<SaveMappingSchemeEntryRequest> Entries, List<CourseStatusRequest> Statuses);

public record SaveMappingSchemeEntryRequest(int Id, int HomeSlotId, int PartnerCourseId, decimal AwardedEcts);

public record CourseStatusRequest(int PartnerCourseId, string? EnrollmentStatus);
