using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class MappingHistoryRepository(AppDbContext context) : Repository<MappingHistory>(context), IMappingHistoryRepository
{
    public async Task<List<MappingHistory>> GetByMappingIdAsync(Guid mappingId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.CourseMappingId == mappingId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<MappingHistory>> GetByExchangeIdAsync(Guid exchangeId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Include(h => h.CourseMapping)
                .ThenInclude(cm => cm.ExchangeCourse)
            .Where(h => h.CourseMapping.ExchangeCourse.ExchangeId == exchangeId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);
    }
}
