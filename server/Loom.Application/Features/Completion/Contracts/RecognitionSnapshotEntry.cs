namespace Loom.Application.Features.Completion;

public record RecognitionSnapshotEntry(
    string HomeSlotLabel,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate,
    bool? IsRecognized,
    string? RecognizedAsCourseName
);
