using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Entities;

public class CourseMapping : AuditableEntity
{
    public Guid ExchangeCourseId { get; set; }
    public Guid CourseId { get; set; }
    public CourseMappingStatus Status { get; set; }
    public string? CoordinatorNote { get; set; }
    public decimal? AwardedEcts { get; set; }
    public string? ConvertedGrade { get; set; }

    public ExchangeCourse ExchangeCourse { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<MappingHistory> MappingHistoryEntries { get; set; } = [];
}
