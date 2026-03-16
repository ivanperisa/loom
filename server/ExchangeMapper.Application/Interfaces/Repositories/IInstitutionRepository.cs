using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IInstitutionRepository : IRepository<Institution>
{
    Task<List<Institution>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<List<Institution>> GetForeignInstitutionsAsync(CancellationToken ct = default);
}
