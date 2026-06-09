namespace Loom.Application.DTOs.Recognition;

public record RecognitionSnapshotEntryChange(RecognitionSnapshotEntry Before, RecognitionSnapshotEntry After);

public record RecognitionSnapshotDiff(
    List<RecognitionSnapshotEntry> Added,
    List<RecognitionSnapshotEntry> Removed,
    List<RecognitionSnapshotEntryChange> Modified
);

public record RecognitionSnapshotSummary(
    int Id,
    DateTime ApprovedAt,
    string ApprovedByName,
    int EntryCount,
    RecognitionSnapshotDiff? Diff
);
