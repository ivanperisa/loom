using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class PartnerCourse : EntityBase
{
    public int InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? NameHr { get; set; }
    public decimal Ects { get; set; }
    public int? LecturesH { get; set; }
    public int? AuditoryH { get; set; }
    public int? LabH { get; set; }
    public ExchangeSemester Semester { get; set; }
    public StudyProgramLevel Level { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
