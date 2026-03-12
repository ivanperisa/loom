using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Entities;

public class Course : EntityBase
{
    public Guid StudyProfileId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int Ects { get; set; }
    public CourseType Type { get; set; }
    public CourseStatus Status { get; set; }
    public int? LecturesHours { get; set; }
    public int? AuditoryHours { get; set; }
    public int? LabHours { get; set; }

    public StudyProfile StudyProfile { get; set; } = null!;
    public ICollection<CourseMapping> CourseMappings { get; set; } = [];
}
