namespace Loom.Application.DTOs.Recognition;

public record RecognitionEntryResponse(
    Guid Id,
    Guid LearningAgreementEntryId,
    string ForeignCourseCode,
    string ForeignCourseNameEn,
    string? ForeignCourseNameHr,
    decimal ForeignCourseEcts,
    string? ForeignCourseHours,
    decimal AwardedEcts,
    string CourseSlotName,
    string? CourseSlotCode,
    string CourseSlotCategoryCode,
    string CourseSlotCategoryName,
    string CourseSlotColor,
    int CourseSlotSemester,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate,
    bool? IsRecognized
);
