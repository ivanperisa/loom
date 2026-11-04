namespace Loom.Application.Features.Completion;

/// <summary>What an approved recognition version stores: the results (placement + grades) with display copies.</summary>
public record RecognitionVersionPayload(List<RecognitionVersionEntry> Entries);

public record RecognitionVersionEntry(
    int HomeSlotId,
    string HomeSlotLabel,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    decimal? AwardedEcts,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate);
