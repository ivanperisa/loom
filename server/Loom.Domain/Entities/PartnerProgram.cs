using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class PartnerProgram : EntityBase
{
    public int InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? NameEn { get; set; }
    public StudyProgramLevel Level { get; set; }

    public ICollection<PartnerCourse> Courses { get; set; } = [];
}
