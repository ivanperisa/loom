using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class CourseRepository(AppDbContext context) : Repository<Course>(context), ICourseRepository
{
    public async Task<List<Course>> GetByStudyProfileAsync(Guid studyProfileId, string? query, CancellationToken ct = default)
    {
        var q = DbSet
            .AsNoTracking()
            .Where(c => c.StudyProfileId == studyProfileId);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(lower)
                || c.NameEn.ToLower().Contains(lower)
                || (c.Code != null && c.Code.ToLower().Contains(lower)));
        }

        return await q.OrderBy(c => c.Name).ToListAsync(ct);
    }
}
