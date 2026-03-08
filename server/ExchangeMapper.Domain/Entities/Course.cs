using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class Course : EntityBase
{
    public Guid StudyProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal Ects { get; set; }

    public StudyProfile StudyProfile { get; set; } = null!;
    public ICollection<CourseMapping> CourseMappings { get; set; } = [];
}
