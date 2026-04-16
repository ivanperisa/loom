using ErrorOr;
using Loom.Domain.Entities;

namespace Loom.Application.Interfaces.Services;

public interface IUserSyncService
{
    Task<ErrorOr<User>> SyncUserAsync(string externalId, string email, string name, CancellationToken ct = default);
}
