using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Entities;

public class Exchange : AuditableEntity
{
    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public Guid StudyProfileId { get; set; }
    public StudyProfile StudyProfile { get; set; } = null!;

    public Guid ForeignProgramId { get; set; }
    public ForeignProgram ForeignProgram { get; set; } = null!;

    public string AcademicYear { get; set; } = null!;
    public ExchangeSemester SemesterType { get; set; }
    public int StudySemester { get; set; }
    public ExchangeStatus Status { get; set; }
    public string? CoordinatorMessage { get; set; }

    public ICollection<SlotState> SlotStates { get; set; } = null!;
    public Recognition? Recognition { get; set; }
    public ICollection<ExchangeSnapshot> Snapshots { get; set; } = null!;
}
