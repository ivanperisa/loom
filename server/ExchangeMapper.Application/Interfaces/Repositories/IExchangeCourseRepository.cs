using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IExchangeCourseRepository : IRepository<ExchangeCourse>
{
    Task<ExchangeCourse?> GetWithMappingsAsync(Guid courseId, CancellationToken ct = default);
}
