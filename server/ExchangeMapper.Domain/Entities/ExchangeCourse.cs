using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Entities;

public class ExchangeCourse : EntityBase
{
    public Guid ExchangeId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal? Ects { get; set; }
    public ExchangeCourseStatus Status { get; set; }
    public int? LecturesHours { get; set; }
    public int? AuditoryHours { get; set; }
    public int? LabHours { get; set; }
    public string? OriginalGrade { get; set; }
    public string? EctsGrade { get; set; }
    public DateOnly? ExamDate { get; set; }

    public Exchange Exchange { get; set; } = null!;
    public ICollection<CourseMapping> CourseMappings { get; set; } = [];
}
