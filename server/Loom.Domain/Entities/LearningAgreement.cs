using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class LearningAgreement : AuditableEntity
{
    public Guid ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public ICollection<LearningAgreementEntry> Entries { get; set; } = null!;
}
