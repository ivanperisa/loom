namespace Loom.Application.Features.Completion;

public record UpsertRecognitionEntryRequest(
    int LearningAgreementEntryId,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate
);
