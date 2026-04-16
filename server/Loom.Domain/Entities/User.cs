using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class User : EntityBase
{
    public string ExternalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsOnboarded { get; set; }
    public string? Jmbag { get; set; }
    public string? Mentor { get; set; }

    public Guid? InstitutionId { get; set; }
    public Institution? Institution { get; set; }

    public Guid? CoordinatorId { get; set; }
    public User? Coordinator { get; set; }

    public string? CoordinatorRequestStatus { get; set; }

    public ICollection<Exchange> StudentExchanges { get; set; } = [];
}
