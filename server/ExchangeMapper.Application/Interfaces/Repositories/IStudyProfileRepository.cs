using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IStudyProfileRepository
{
    Task<StudyProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<StudyProfile>> GetByProgramIdAsync(Guid programId, CancellationToken ct = default);
    Task AddAsync(StudyProfile profile);
}
