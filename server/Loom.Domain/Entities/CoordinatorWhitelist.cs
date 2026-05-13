using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class CoordinatorWhitelist : EntityBase
{
    public string Email { get; set; } = string.Empty;
    public int? InstitutionId { get; set; }
    public Institution? Institution { get; set; }
}
