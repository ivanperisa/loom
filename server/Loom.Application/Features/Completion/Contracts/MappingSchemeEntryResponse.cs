using Loom.Domain.Enums;

namespace Loom.Application.Features.Completion;

public record MappingSchemeEntryResponse(
    int Id,
    int HomeSlotId,
    int? PartnerCourseId,
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
