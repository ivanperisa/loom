namespace Loom.Application.DTOs.Recognition;

public record RecognitionEntryResponse(
    Guid Id,
    Guid SlotMappingId,
    string ForeignCourseCode,
    string ForeignCourseNameEn,
    decimal AwardedEcts,
    string CourseSlotName,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate
);
