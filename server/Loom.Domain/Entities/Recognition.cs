using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class Recognition : AuditableEntity
{
    public int ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public DocumentStatus Status { get; set; }

    public ICollection<RecognitionEntry> Entries { get; set; } = null!;
}
