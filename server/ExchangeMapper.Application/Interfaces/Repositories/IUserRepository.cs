using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<User?> GetByExternalIdWithDetailsAsync(string externalId, CancellationToken ct = default);
}
