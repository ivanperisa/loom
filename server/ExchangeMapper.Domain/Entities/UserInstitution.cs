using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class UserInstitution : EntityBase
{
    public Guid UserId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid? StudyProfileId { get; set; }

    public User User { get; set; } = null!;
    public Institution Institution { get; set; } = null!;
    public StudyProfile? StudyProfile { get; set; }
    public ICollection<Exchange> Exchanges { get; set; } = [];
}
