using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IInstitutionRepository
{
    Task<List<Institution>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<Institution?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Institution institution);
}
