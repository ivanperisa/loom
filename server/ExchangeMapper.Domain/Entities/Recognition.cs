using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Entities;

public class Recognition : AuditableEntity
{
    public Guid ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public RecognitionStatus Status { get; set; }
    public ICollection<RecognitionEntry> Entries { get; set; } = null!;
}
