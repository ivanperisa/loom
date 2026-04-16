namespace Loom.Domain.Common;

public class AuditableEntity : EntityBase
{
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
