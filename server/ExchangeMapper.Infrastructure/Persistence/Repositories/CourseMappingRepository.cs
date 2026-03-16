using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class CourseMappingRepository(AppDbContext context) : Repository<CourseMapping>(context), ICourseMappingRepository
{
    public async Task<CourseMapping?> GetWithCourseAsync(Guid mappingId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == mappingId, ct);
    }

    public Task DeleteRangeAsync(IEnumerable<CourseMapping> mappings, CancellationToken ct = default)
    {
        DbSet.RemoveRange(mappings);
        return Task.CompletedTask;
    }
}
