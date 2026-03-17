using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Interfaces.Repositories;

public interface IExchangeRepository : IRepository<Exchange>
{
    Task<Exchange?> GetByStudentIdAsync(Guid studentId, CancellationToken ct = default);
    Task<bool> ExistsForStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<bool> ExistsForUserInstitutionAsync(Guid userInstitutionId, CancellationToken ct = default);
    Task<List<Exchange>> GetAllWithStudentAsync(CancellationToken ct = default);
    Task<List<Exchange>> GetByMentorNameAsync(string mentorName, CancellationToken ct = default);
}
