using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class CourseMapping : AuditableEntity
{
    public Guid ExchangeCourseId { get; set; }
    public Guid CourseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CoordinatorNote { get; set; }

    public ExchangeCourse ExchangeCourse { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<MappingHistory> MappingHistoryEntries { get; set; } = [];
}
