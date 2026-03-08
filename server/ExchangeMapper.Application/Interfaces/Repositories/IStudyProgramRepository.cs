using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IStudyProgramRepository
{
    Task<StudyProgram?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<StudyProgram>> GetByInstitutionIdAsync(Guid institutionId, CancellationToken ct = default);
    Task AddAsync(StudyProgram program);
}
