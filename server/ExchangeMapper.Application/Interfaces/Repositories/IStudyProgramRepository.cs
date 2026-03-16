using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IStudyProgramRepository : IRepository<StudyProgram>
{
    Task<List<StudyProgram>> GetByInstitutionIdAsync(Guid institutionId, CancellationToken ct = default);
}
