namespace Loom.Application.DTOs.Recognition;

public record RecognitionEntryResponse(
    int Id,
    int LearningAgreementEntryId,
    string PartnerCourseCode,
    string PartnerCourseNameEn,
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
