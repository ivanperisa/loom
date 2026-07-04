namespace Loom.Application.DTOs.MappingScheme;

public record MappingSchemeEntryResponse(
    int Id,
    int HomeSlotId,
    int? PartnerCourseId,
    string PartnerCourseCode,
    string PartnerCourseName,
    string? PartnerCourseNameHr,
    string? PartnerCourseHours,
    decimal PartnerCourseEcts,
    int? HomeSlotCourseIsvuCode,
    string HomeSlotCourseName,
    int? HomeSlotCourseGroupIsvuCode,
    string HomeSlotCourseGroupName,
    string HomeSlotColor,
    int HomeSlotSemester,
    decimal AwardedEcts,
    int? RecognizedAsCourseId,
    string? RecognizedAsCourseName,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate,
    bool? IsRecognized
);
