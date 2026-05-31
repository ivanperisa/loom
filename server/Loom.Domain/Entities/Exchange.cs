using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class Exchange : AuditableEntity
{
    public Guid Guid { get; set; }

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public int HomeProfileId { get; set; }
    public HomeProfile HomeProfile { get; set; } = null!;

    public int PartnerProgramId { get; set; }
    public PartnerProgram PartnerProgram { get; set; } = null!;

    public int? CoordinatorId { get; set; }
    public User? Coordinator { get; set; }

    public string AcademicYear { get; set; } = null!;
    public ExchangeSemester SemesterType { get; set; }
    public List<int> StudySemesters { get; set; } = [];
    public string? CoordinatorMessage { get; set; }

    public LearningAgreement? LearningAgreement { get; set; }
    public Recognition? Recognition { get; set; }
    public ICollection<ExchangeSnapshot> Snapshots { get; set; } = null!;
}
