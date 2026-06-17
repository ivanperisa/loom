namespace Loom.Application.DTOs.LearningAgreement;

public record LaSnapshotSummary(
    int Id,
    DateTime ApprovedAt,
    string ApprovedByName,
    int EntryCount,
    LaSnapshotDiff? Diff
);
