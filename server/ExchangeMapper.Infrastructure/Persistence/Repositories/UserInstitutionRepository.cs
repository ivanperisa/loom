using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class UserInstitutionRepository(AppDbContext context) : IUserInstitutionRepository
{
    public async Task<List<UserInstitution>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.UserInstitutions
            .AsNoTracking()
            .Include(ui => ui.Institution)
            .Include(ui => ui.StudyProfile)
                .ThenInclude(sp => sp!.StudyProgram)
            .Include(ui => ui.Exchanges)
            .Where(ui => ui.UserId == userId)
            .OrderBy(ui => ui.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<UserInstitution?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.UserInstitutions
            .FirstOrDefaultAsync(ui => ui.Id == id, ct);
    }

    public Task AddAsync(UserInstitution userInstitution)
    {
        context.UserInstitutions.Add(userInstitution);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserInstitution userInstitution)
    {
        context.UserInstitutions.Update(userInstitution);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserInstitution userInstitution)
    {
        context.UserInstitutions.Remove(userInstitution);
        return Task.CompletedTask;
    }
}
