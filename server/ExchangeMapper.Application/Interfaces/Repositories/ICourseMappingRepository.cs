using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface ICourseMappingRepository : IRepository<CourseMapping>
{
    Task<CourseMapping?> GetWithCourseAsync(Guid mappingId, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<CourseMapping> mappings, CancellationToken ct = default);
}
