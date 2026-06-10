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

    public ICollection<RecognitionEntry> Entries { get; set; } = null!;
}
