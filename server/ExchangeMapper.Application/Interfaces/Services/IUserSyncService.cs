using ErrorOr;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IUserSyncService
{
    Task<ErrorOr<User>> SyncUserAsync(string externalId, string email, string name, CancellationToken ct = default);
}
