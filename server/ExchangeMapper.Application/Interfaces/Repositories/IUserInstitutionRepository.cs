using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IUserInstitutionRepository : IRepository<UserInstitution>
{
    Task<List<UserInstitution>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserInstitution?> GetHomeByUserIdAsync(Guid userId, CancellationToken ct = default);
}
