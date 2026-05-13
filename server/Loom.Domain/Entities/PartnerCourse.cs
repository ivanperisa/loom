using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class PartnerCourse : EntityBase
{
    public int ProgramId { get; set; }
    public PartnerProgram Program { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? NameHr { get; set; }
    public decimal Ects { get; set; }
    public int? LecturesH { get; set; }
    public int? AuditoryH { get; set; }
    public int? LabH { get; set; }
}
