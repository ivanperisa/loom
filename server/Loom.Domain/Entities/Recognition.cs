using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class Recognition : AuditableEntity
{
    public int ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public DocumentStatus Status { get; set; }
    public string? Message { get; set; }

    public int? LastModifiedById { get; set; }
    public User? LastModifiedByUser { get; set; }

    public int? SignedById { get; set; }
    public User? SignedByUser { get; set; }
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// "Start final recognition": from here on the LA and the agreed mapping (table 1) are frozen for good; only the
    /// results (table 2 and the mapping scheme) change. The row itself can exist earlier, for notes.
    /// </summary>
    public DateTime? StartedAt { get; set; }
    public int? StartedById { get; set; }
    public User? StartedByUser { get; set; }

    public bool IsStarted => StartedAt is not null;

    public ICollection<RecognitionEntry> Entries { get; set; } = [];
}
