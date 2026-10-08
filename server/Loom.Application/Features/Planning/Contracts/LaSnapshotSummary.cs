namespace Loom.Application.Features.Planning;

public record LaSnapshotSummary(
    int Id,
    DateTime ApprovedAt,
    string ApprovedByName,
    int EntryCount,
    LaSnapshotDiff? Diff
);
