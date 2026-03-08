using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class MappingHistory : EntityBase
{
    public Guid CourseMappingId { get; set; }
    public Guid ChangedBy { get; set; }
    public string Snapshot { get; set; } = string.Empty;

    public CourseMapping CourseMapping { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
