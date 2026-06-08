using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class HomeProgram : EntityBase
{
    public int InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public StudyProgramLevel Level { get; set; }
    public int DurationSemesters { get; set; }

    public ICollection<HomeProfile> Profiles { get; set; } = [];
}
