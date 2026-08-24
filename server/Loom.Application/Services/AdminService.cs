using System.Linq.Expressions;
using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.Common;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class AdminService(IAppDbContext db, CachedQuery cache) : IAdminService
{
    private static readonly TimeSpan ListTtl = TimeSpan.FromSeconds(30);

    #region Users

    public async Task<ErrorOr<PagedResponse<UserListResponse>>> GetAllUsersAsync(int adminId, PagedRequest paging, UserRole? role = null, int? institutionId = null, bool? registered = null, string? sortBy = null, CancellationToken ct = default)
    {
        var key = $"{role}:{institutionId}:{registered}:{sortBy}:{paging.SortDir}:{paging.Search}:{paging.SafePage}:{paging.SafePageSize}";
        return await cache.GetOrCreateAsync("users", key, ListTtl, async () =>
        {
            var query = db.Users.AsNoTracking();

            if (role is not null)
                query = query.Where(u => u.Role == role.Value);

            if (institutionId is not null)
                query = query.Where(u => u.InstitutionId == institutionId.Value);

            if (registered is not null)
                query = registered.Value
                    ? query.Where(u => u.Email != "")
                    : query.Where(u => u.Email == "");

            if (!string.IsNullOrWhiteSpace(paging.Search))
            {
                var term = $"%{paging.Search.Trim().ToLower()}%";
                query = query.Where(u =>
                    EF.Functions.Like(u.Name.ToLower(), term) ||
                    EF.Functions.Like(u.Email.ToLower(), term) ||
                    (u.Jmbag != null && EF.Functions.Like(u.Jmbag.ToLower(), term)) ||
                    (u.Institution != null && EF.Functions.Like(u.Institution.Name.ToLower(), term)));
            }

            var desc = paging.SortDir == "desc";
            Func<IQueryable<User>, IOrderedQueryable<User>> orderBy = sortBy switch
            {
                "role" => q => desc ? q.OrderByDescending(u => u.Role) : q.OrderBy(u => u.Role),
                "jmbag" => q => desc ? q.OrderByDescending(u => u.Jmbag) : q.OrderBy(u => u.Jmbag),
                _ => q => desc ? q.OrderByDescending(u => u.Name) : q.OrderBy(u => u.Name),
            };

            return await query.ToProjectedPagedResponseAsync(
                paging,
                orderBy,
                u => u.Id,
                UserListProjection,
                ct);
        });
    }

    public async Task<ErrorOr<UserListResponse>> UpdateUserAsync(int adminId, int targetUserId, AdminUpdateUserRequest request, CancellationToken ct = default)
    {
        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (!string.IsNullOrWhiteSpace(request.Jmbag))
        {
            var jmbagTaken = await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag && u.Id != targetUserId, ct);
            if (jmbagTaken) return Error.Conflict("JMBAG_TAKEN", "A student with this JMBAG already exists.");
        }

        target.Name = request.Name;
        target.Jmbag = request.Jmbag;
        target.Mentor = request.Mentor;
        target.InstitutionId = request.InstitutionId;

        if (!target.CanActAsCoordinator())
        {
            var setCoordinator = await db.SetStudentCoordinatorAsync(target, request.CoordinatorId, ct);
            if (setCoordinator.IsError) return setCoordinator.Errors;
        }
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("users");

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();

        return ToUserListResponse(saved);
    }

    #endregion

    #region Coordinator role management

    public async Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(int adminId, CancellationToken ct = default)
    {
        var requests = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .Where(u => u.CoordinatorRequestStatus == "Pending" && u.Role == UserRole.Student)
            .Select(u => new CoordinatorRequestResponse(u.Id, u.Name, u.Email, u.Institution != null ? u.Institution.Name : null))
            .ToListAsync(ct);

        return requests;
    }

    public async Task<ErrorOr<UserListResponse>> SetUserRoleAsync(int adminId, int targetUserId, UserRole role, CancellationToken ct = default)
    {
        if (targetUserId == adminId)
            return Error.Validation("CANNOT_CHANGE_OWN_ROLE", "You cannot change your own role.");

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (target.Role != role)
        {
            if (role == UserRole.Student && target.CanActAsCoordinator())
            {
                var students = await db.Users.Where(u => u.CoordinatorId == target.Id).ToListAsync(ct);
                foreach (var s in students)
                    s.CoordinatorId = null;

                var exchanges = await db.Exchanges.Where(e => e.CoordinatorId == target.Id).ToListAsync(ct);
                foreach (Exchange ex in exchanges)
                    ex.CoordinatorId = null;

                var whitelistEntry = await db.CoordinatorWhitelist
                    .FirstOrDefaultAsync(e => e.Email == target.Email.ToLowerInvariant(), ct);
                if (whitelistEntry is not null)
                    db.CoordinatorWhitelist.Remove(whitelistEntry);
            }

            if (role != UserRole.Student)
                target.CoordinatorId = null;

            target.Role = role;
            target.CoordinatorRequestStatus = null;
            await db.SaveChangesAsync(ct);
            cache.BumpVersion("users");
            cache.BumpVersion("coordinators");
        }

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return ToUserListResponse(saved);
    }

    public async Task<ErrorOr<AuthMeResponse>> RejectCoordinatorRequestAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (target.CoordinatorRequestStatus != "Pending")
            return Error.Validation("NO_PENDING_REQUEST", "User does not have a pending coordinator request.");

        target.CoordinatorRequestStatus = "Rejected";
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("users");

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToAuthMeResponse();
    }

    #endregion

    #region Coordinator whitelist

    public async Task<ErrorOr<List<CoordinatorWhitelistEntryResponse>>> GetCoordinatorWhitelistAsync(int adminId, CancellationToken ct = default)
    {
        var entries = await db.CoordinatorWhitelist
            .AsNoTracking()
            .OrderBy(e => e.Email)
            .Select(e => new CoordinatorWhitelistEntryResponse(e.Id, e.Email, e.CreatedAt))
            .ToListAsync(ct);

        return entries;
    }

    public async Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation("INVALID_EMAIL", "Email is required.");

        var exists = await db.CoordinatorWhitelist.AnyAsync(e => e.Email == email.ToLowerInvariant(), ct);
        if (exists) return Error.Conflict("EMAIL_ALREADY_WHITELISTED", "This email is already on the coordinator whitelist.");

        var entry = new CoordinatorWhitelist { Email = email.Trim().ToLowerInvariant() };
        db.CoordinatorWhitelist.Add(entry);
        await db.SaveChangesAsync(ct);

        return new CoordinatorWhitelistEntryResponse(entry.Id, entry.Email, entry.CreatedAt);
    }

    public async Task<ErrorOr<Deleted>> RemoveFromCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default)
    {
        var entry = await db.CoordinatorWhitelist.FirstOrDefaultAsync(e => e.Email == email.ToLowerInvariant(), ct);
        if (entry is null) return Error.NotFound("EMAIL_NOT_FOUND", "Email not found on the coordinator whitelist.");

        db.CoordinatorWhitelist.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }

    #endregion

    #region Private methods

    private IQueryable<User> UsersWithIncludes() => db.Users
        .Include(u => u.Institution)
        .Include(u => u.Coordinator);

    private static readonly Expression<Func<User, UserListResponse>> UserListProjection = u => new UserListResponse(
        u.Id,
        u.Name,
        u.Email,
        u.Role.ToString(),
        u.Institution != null ? u.Institution.Name : null,
        u.Institution != null ? u.Institution.City : null,
        u.InstitutionId,
        u.CoordinatorRequestStatus,
        u.IsOnboarded,
        u.Jmbag,
        u.Mentor,
        u.CoordinatorId,
        u.Coordinator != null ? u.Coordinator.Name : null);

    private static readonly Func<User, UserListResponse> ToUserListResponse = UserListProjection.Compile();

    #endregion
}
