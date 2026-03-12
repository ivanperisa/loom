using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Entities;

public class Exchange : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid UserInstitutionId { get; set; }
    public Guid ForeignInstitutionId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public ExchangeSemester Semester { get; set; }
    public ExchangeStatus Status { get; set; }
    public int? DurationMonths { get; set; }
    public string? Mentor { get; set; }

    public User Student { get; set; } = null!;
    public UserInstitution UserInstitution { get; set; } = null!;
    public Institution ForeignInstitution { get; set; } = null!;
    public ICollection<ExchangeCourse> ExchangeCourses { get; set; } = [];
}
