using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class StudyProgramRepository(AppDbContext context) : Repository<StudyProgram>(context), IStudyProgramRepository
{
    public async Task<List<StudyProgram>> GetByInstitutionIdAsync(Guid institutionId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }
}
