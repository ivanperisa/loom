using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class StudyProgram : EntityBase
{
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public StudyProgramLevel Level { get; set; }
    public int DurationSemesters { get; set; }
    public Institution Institution { get; set; } = null!;
    public ICollection<StudyProfile> StudyProfiles { get; set; } = [];
}
