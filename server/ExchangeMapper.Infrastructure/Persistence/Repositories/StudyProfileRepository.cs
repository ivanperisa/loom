using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class StudyProfileRepository(AppDbContext context) : Repository<StudyProfile>(context), IStudyProfileRepository
{
    public async Task<List<StudyProfile>> GetByProgramIdAsync(Guid programId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.StudyProgramId == programId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }
}
