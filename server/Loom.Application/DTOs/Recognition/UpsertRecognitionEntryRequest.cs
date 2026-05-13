namespace Loom.Application.DTOs.Recognition;

public record UpsertRecognitionEntryRequest(
    int LearningAgreementEntryId,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate
);

public record SetEntryRecognizedRequest(bool? IsRecognized);
