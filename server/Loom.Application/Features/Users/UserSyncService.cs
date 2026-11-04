using ErrorOr;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Users;

/// <summary>Creates or updates the local user for an external (Google / dev) login.</summary>
public sealed class UserSyncService(IAppDbContext db)
{
    public async Task<ErrorOr<SyncedUser>> SyncAsync(string externalId, string email, string name, CancellationToken ct)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);

        if (user is null)
        {
            var whitelisted = await db.CoordinatorWhitelist.AnyAsync(e => e.Email == normalizedEmail, ct);
            user = new User
            {
                ExternalId = externalId,
                Email = email,
                Name = name,
                Role = whitelisted ? UserRole.Coordinator : UserRole.Student,
                IsOnboarded = false,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        else if (!user.IsOnboarded && user.Role == UserRole.Student
            && await db.CoordinatorWhitelist.AnyAsync(e => e.Email == normalizedEmail, ct))
        {
            // Whitelisted after the first login, but before onboarding.
            user.Role = UserRole.Coordinator;
            user.CoordinatorRequestStatus = null;
            await db.SaveChangesAsync(ct);
        }

        return new SyncedUser(user.Id, user.Role);
    }
}
