using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class Exchange : AuditableEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public Guid StudyProfileId { get; set; }
    public StudyProfile StudyProfile { get; set; } = null!;

    public Guid ForeignProgramId { get; set; }
    public ForeignProgram ForeignProgram { get; set; } = null!;

    public Guid? CoordinatorId { get; set; }
    public User? Coordinator { get; set; }

    public string AcademicYear { get; set; } = null!;
    public ExchangeSemester SemesterType { get; set; }
    public int StudySemester { get; set; }
    public string? CoordinatorMessage { get; set; }

    public LearningAgreement? LearningAgreement { get; set; }
    public Recognition? Recognition { get; set; }
    public ICollection<ExchangeSnapshot> Snapshots { get; set; } = null!;
}
