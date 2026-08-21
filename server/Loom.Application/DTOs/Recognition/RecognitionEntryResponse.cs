namespace Loom.Application.DTOs.Recognition;

public record RecognitionEntryResponse(
    int Id,
    int LearningAgreementEntryId,
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
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate
);
