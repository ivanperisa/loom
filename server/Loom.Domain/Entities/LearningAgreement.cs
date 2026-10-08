using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class LearningAgreement : AuditableEntity
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
    /// "Start final recognition": from here on the LA and the agreed mapping (recognition table 1) are frozen for good;
    /// only the results (table 2 and the mapping scheme) change.
    /// </summary>
    public DateTime? ConcludedAt { get; set; }
    public int? ConcludedById { get; set; }
    public User? ConcludedByUser { get; set; }

    public bool IsConcluded => ConcludedAt is not null;

    public ICollection<LearningAgreementEntry> Entries { get; set; } = null!;
}
