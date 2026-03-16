using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface ICourseRepository : IRepository<Course>
{
    Task<List<Course>> GetByStudyProfileAsync(Guid studyProfileId, string? query, CancellationToken ct = default);
}
