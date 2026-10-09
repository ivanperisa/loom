using Loom.Domain.Enums;

namespace Loom.Application.Features.Completion;

/// <summary>A placement (slot + ECTS) with its course's result, which every placement of that course shares.</summary>
public record MappingSchemeEntryResponse(
    int Id,
    int HomeSlotId,
    int PartnerCourseId,
    string PartnerCourseCode,
    string PartnerCourseName,
    string? PartnerCourseNameHr,
    string? PartnerCourseUrl,
    string? PartnerCourseHours,
    decimal PartnerCourseEcts,
    int? HomeSlotCourseIsvuCode,
    string HomeSlotCourseName,
    int? HomeSlotCourseGroupIsvuCode,
    string HomeSlotCourseGroupName,
    string HomeSlotColor,
    int HomeSlotSemester,
    decimal AwardedEcts,
    EnrollmentStatus? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate
);
