using System.Linq.Expressions;
using ErrorOr;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Querying;
using Loom.Application.Common.Security;
using Loom.Application.Features.Users;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Loom.Application.Features.Admin;

public sealed class AdminUserService(IAppDbContext db, ICurrentActor actor, IMemoryCache memoryCache)
{
    private static readonly Expression<Func<User, UserListResponse>> Projection = u => new UserListResponse(
        u.Id, u.Name, u.Email, u.Role.ToString(),
        u.Institution != null ? u.Institution.Name : null,
        u.Institution != null ? u.Institution.City : null,
        u.InstitutionId, u.CoordinatorRequestStatus, u.IsOnboarded, u.Jmbag, u.Mentor,
        u.CoordinatorId, u.Coordinator != null ? u.Coordinator.Name : null);

    private static readonly ListSpec<User, UserListResponse> List = ListSpec.For<User>()
        .SearchIn(u => u.Name, u => u.Email, u => u.Jmbag, u => u.Institution!.Name)
        .SortBy("name", u => u.Name, isDefault: true)
        .SortBy("role", u => u.Role)
        .SortBy("jmbag", u => u.Jmbag)
        .Project(Projection);

    public Task<PagedResponse<UserListResponse>> ListAsync(UserListQuery query, CancellationToken ct) =>
        db.Users
            .AsNoTracking()
            .WhereIf(query.Role is not null, u => u.Role == query.Role)
            .WhereIf(query.InstitutionId is not null, u => u.InstitutionId == query.InstitutionId)
            .WhereIf(query.Registered == true, u => u.Email != "")
            .WhereIf(query.Registered == false, u => u.Email == "")
            .ToPageAsync(List, query, ct);

    public async Task<ErrorOr<UserListResponse>> UpdateAsync(int userId, AdminUpdateUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return CommonErrors.UserNotFound;

        if (!string.IsNullOrWhiteSpace(request.Jmbag) && await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag && u.Id != userId, ct))
            return AdminErrors.JmbagTaken;

        user.Name = request.Name;
        user.Jmbag = request.Jmbag;
        user.Mentor = request.Mentor;
        user.InstitutionId = request.InstitutionId;

        if (!user.CanActAsCoordinator())
        {
            var assigned = await db.AssignCoordinatorAsync(user, request.CoordinatorId, ct);
            if (assigned.IsError) return assigned.Errors;
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<ErrorOr<UserListResponse>> SetRoleAsync(int userId, UserRole role, CancellationToken ct)
    {
        if (userId == actor.UserId) return AdminErrors.CannotChangeOwnRole;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return CommonErrors.UserNotFound;

        if (user.Role != role)
        {
            if (role == UserRole.Student && user.CanActAsCoordinator())
                await DetachCoordinatorAsync(user, ct);
            if (role != UserRole.Student)
                user.CoordinatorId = null;

            user.Role = role;
            user.CoordinatorRequestStatus = null;
            await db.SaveChangesAsync(ct);

            // The API caches each user's role for a few minutes; drop it so the change applies immediately.
            memoryCache.Remove(UserSyncCache.Key(user.ExternalId));
        }

        return await GetAsync(userId, ct);
    }

    /// <summary>A coordinator becoming a student loses their students, exchanges and whitelist entry.</summary>
    private async Task DetachCoordinatorAsync(User coordinator, CancellationToken ct)
    {
        foreach (var student in await db.Users.Where(u => u.CoordinatorId == coordinator.Id).ToListAsync(ct))
            student.CoordinatorId = null;
        foreach (var exchange in await db.Exchanges.Where(e => e.CoordinatorId == coordinator.Id).ToListAsync(ct))
            exchange.CoordinatorId = null;

        var email = coordinator.Email.ToLowerInvariant();
        var whitelistEntry = await db.CoordinatorWhitelist.FirstOrDefaultAsync(e => e.Email == email, ct);
        if (whitelistEntry is not null) db.CoordinatorWhitelist.Remove(whitelistEntry);
    }

    private Task<UserListResponse> GetAsync(int userId, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(u => u.Id == userId).Select(Projection).FirstAsync(ct);
}
