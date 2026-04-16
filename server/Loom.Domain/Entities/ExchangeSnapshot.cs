using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class ExchangeSnapshot : EntityBase
{
    public Guid ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public Guid ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;
    public SnapshotPhase Phase { get; set; }
    public string Snapshot { get; set; } = null!;
}
