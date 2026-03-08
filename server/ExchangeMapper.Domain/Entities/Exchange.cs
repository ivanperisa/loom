using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class Exchange : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid UserInstitutionId { get; set; }
    public Guid ForeignInstitutionId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public User Student { get; set; } = null!;
    public UserInstitution UserInstitution { get; set; } = null!;
    public Institution ForeignInstitution { get; set; } = null!;
    public ICollection<ExchangeCourse> ExchangeCourses { get; set; } = [];
}
