using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class StudyProgramRepository(AppDbContext context) : IStudyProgramRepository
{
    public async Task<StudyProgram?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.StudyPrograms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<List<StudyProgram>> GetByInstitutionIdAsync(Guid institutionId, CancellationToken ct = default)
    {
        return await context.StudyPrograms
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public Task AddAsync(StudyProgram program)
    {
        context.StudyPrograms.Add(program);
        return Task.CompletedTask;
    }
}
