using Loom.Domain.Enums;

namespace Loom.Application.Features.Planning;

public record SnapshotListItem(
    int Id,
    SnapshotType Type,
    DateTime CreatedAt,
    string CreatedByName,
    int EntryCount
);
