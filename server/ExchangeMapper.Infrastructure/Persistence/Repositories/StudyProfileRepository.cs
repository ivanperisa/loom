using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class StudyProfileRepository(AppDbContext context) : IStudyProfileRepository
{
    public async Task<StudyProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.StudyProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<StudyProfile>> GetByProgramIdAsync(Guid programId, CancellationToken ct = default)
    {
        return await context.StudyProfiles
            .AsNoTracking()
            .Where(x => x.StudyProgramId == programId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public Task AddAsync(StudyProfile profile)
    {
        context.StudyProfiles.Add(profile);
        return Task.CompletedTask;
    }
}
