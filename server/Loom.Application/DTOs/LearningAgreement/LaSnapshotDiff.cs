namespace Loom.Application.DTOs.LearningAgreement;

public record LaSnapshotEntryChange(LaSnapshotEntry Before, LaSnapshotEntry After);

public record LaSnapshotDiff(
    List<LaSnapshotEntry> Added,
    List<LaSnapshotEntry> Removed,
    List<LaSnapshotEntryChange> Modified
);
