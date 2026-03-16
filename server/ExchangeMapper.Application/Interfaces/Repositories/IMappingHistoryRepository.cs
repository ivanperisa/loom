using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IMappingHistoryRepository : IRepository<MappingHistory>
{
    Task<List<MappingHistory>> GetByMappingIdAsync(Guid mappingId, CancellationToken ct = default);
    Task<List<MappingHistory>> GetByExchangeIdAsync(Guid exchangeId, CancellationToken ct = default);
}
