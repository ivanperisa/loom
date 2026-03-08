using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<User?> GetByExternalIdWithDetailsAsync(string externalId, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
