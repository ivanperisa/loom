using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class ForeignCourse : EntityBase
{
    public Guid ForeignProgramId { get; set; }
    public ForeignProgram ForeignProgram { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string? NameHr { get; set; }
    public decimal Ects { get; set; }
    public int? LecturesH { get; set; }
    public int? AuditoryH { get; set; }
    public int? LabH { get; set; }
}
