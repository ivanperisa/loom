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

    public int? InstitutionId { get; set; }
    public Institution? Institution { get; set; }

    public int? CoordinatorId { get; set; }
    public User? Coordinator { get; set; }

    public CoordinatorRequestStatus? CoordinatorRequestStatus { get; set; }

    public ICollection<Exchange> StudentExchanges { get; set; } = [];

    /// <summary>Created by a coordinator for a student who has no account yet (reached through an access link).</summary>
    public bool IsPlaceholder => Email.Length == 0;

    public bool CanActAsCoordinator() => Role == UserRole.Coordinator || Role == UserRole.Admin;

    public bool IsCoordinatorFor(int? studentCoordinatorId) =>
        studentCoordinatorId == Id;
}
