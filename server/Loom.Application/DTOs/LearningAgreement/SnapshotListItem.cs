using Loom.Domain.Enums;

namespace Loom.Application.DTOs.LearningAgreement;

public record SnapshotListItem(
    int Id,
    SnapshotType Type,
    DateTime CreatedAt,
    string CreatedByName,
    int EntryCount
);
