using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class ExchangeCourseRepository(AppDbContext context) : Repository<ExchangeCourse>(context), IExchangeCourseRepository
{
    public async Task<ExchangeCourse?> GetWithMappingsAsync(Guid courseId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(c => c.CourseMappings)
                .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
    }
}
