using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IStudyProfileRepository : IRepository<StudyProfile>
{
    Task<List<StudyProfile>> GetByProgramIdAsync(Guid programId, CancellationToken ct = default);
}
