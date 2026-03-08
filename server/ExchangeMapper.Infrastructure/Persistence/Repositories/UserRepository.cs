using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        return await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.ExternalId == externalId, ct);
    }

    public async Task<User?> GetByExternalIdWithDetailsAsync(string externalId, CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .Include(u => u.UserInstitutions)
                .ThenInclude(ui => ui.Institution)
            .Include(u => u.UserInstitutions)
                .ThenInclude(ui => ui.StudyProfile)
                    .ThenInclude(sp => sp!.StudyProgram)
            .Include(u => u.UserInstitutions)
                .ThenInclude(ui => ui.Exchanges)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task AddAsync(User user)
    {
        context.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        return Task.CompletedTask;
    }
}
