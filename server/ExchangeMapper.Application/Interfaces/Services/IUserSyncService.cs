using ErrorOr;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IUserSyncService
{
    Task<ErrorOr<User>> SyncUserAsync(string externalId, string email, string name, CancellationToken ct = default);
    Task<ErrorOr<User>> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<ErrorOr<User>> GetByExternalIdWithDetailsAsync(string externalId, CancellationToken ct = default);
}
