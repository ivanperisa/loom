using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class ExchangeCourse : EntityBase
{
    public Guid ExchangeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal Ects { get; set; }

    public Exchange Exchange { get; set; } = null!;
    public ICollection<CourseMapping> CourseMappings { get; set; } = [];
}
