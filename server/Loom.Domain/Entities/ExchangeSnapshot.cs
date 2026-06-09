using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class ExchangeSnapshot : EntityBase
{
    public int ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public int ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;
    public SnapshotPhase Phase { get; set; }
    public SnapshotType Type { get; set; } = SnapshotType.Auto;
    public string Snapshot { get; set; } = null!;
}
