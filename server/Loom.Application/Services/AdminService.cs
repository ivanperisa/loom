using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class AdminService(IAppDbContext db) : IAdminService
{
    #region Users

    public async Task<ErrorOr<List<UserListResponse>>> GetAllUsersAsync(int adminId, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "list users", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var users = await UsersWithIncludes()
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

        return ToUserListResponses(users);
    }

    public async Task<ErrorOr<UserListResponse>> UpdateUserAsync(int adminId, int targetUserId, AdminUpdateUserRequest request, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "update users", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        target.Name = request.Name;
        target.Jmbag = request.Jmbag;
        target.Mentor = request.Mentor;
        target.InstitutionId = request.InstitutionId;
        if (target.Role != UserRole.Coordinator)
        {
            target.CoordinatorId = request.CoordinatorId;
            await db.ReassignUnapprovedExchangesAsync(target.Id, request.CoordinatorId, ct);
        }
        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();

        return ToUserListResponses([saved])[0];
    }

    #endregion

    #region Coordinator role management

    public async Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(int adminId, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "view coordinator requests", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var requests = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .Where(u => u.CoordinatorRequestStatus == "Pending" && u.Role == UserRole.Student)
            .Select(u => new CoordinatorRequestResponse(u.Id, u.Name, u.Email, u.Institution != null ? u.Institution.Name : null))
            .ToListAsync(ct);

        return requests;
    }

    public async Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "assign coordinator role", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        target.Role = UserRole.Coordinator;
        target.CoordinatorRequestStatus = null;
        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> RejectCoordinatorRequestAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "reject coordinator requests", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (target.CoordinatorRequestStatus != "Pending")
            return Error.Validation("NO_PENDING_REQUEST", "User does not have a pending coordinator request.");

        target.CoordinatorRequestStatus = "Rejected";
        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> RemoveCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "remove coordinator role", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (target.Role != UserRole.Coordinator)
            return Error.Validation("NOT_COORDINATOR", "User is not a coordinator.");

        target.Role = UserRole.Student;
        target.CoordinatorRequestStatus = null;
        await db.SaveChangesAsync(ct);

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
        var ensureAdmin = await EnsureAdminAsync(adminId, "view the coordinator whitelist", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var entries = await db.CoordinatorWhitelist
            .AsNoTracking()
            .OrderBy(e => e.Email)
            .Select(e => new CoordinatorWhitelistEntryResponse(e.Id, e.Email, e.CreatedAt))
            .ToListAsync(ct);

        return entries;
    }

    public async Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "manage the coordinator whitelist", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

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
        var ensureAdmin = await EnsureAdminAsync(adminId, "manage the coordinator whitelist", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        var entry = await db.CoordinatorWhitelist.FirstOrDefaultAsync(e => e.Email == email.ToLowerInvariant(), ct);
        if (entry is null) return Error.NotFound("EMAIL_NOT_FOUND", "Email not found on the coordinator whitelist.");

        db.CoordinatorWhitelist.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }

    #endregion

    #region Raw SQL

    public async Task<ErrorOr<SqlExecutionResult>> ExecuteSqlAsync(int adminId, string sql, CancellationToken ct = default)
    {
        var ensureAdmin = await EnsureAdminAsync(adminId, "execute raw SQL", ct);
        if (ensureAdmin.IsError) return ensureAdmin.Errors;

        if (string.IsNullOrWhiteSpace(sql))
            return Error.Validation("INVALID_SQL", "SQL is required.");

        try
        {
            return await db.ExecuteSqlAsync(sql, ct);
        }
        catch (Exception ex)
        {
            return Error.Failure("SQL_ERROR", ex.Message);
        }
    }

    #endregion

    #region Private methods

    private IQueryable<User> UsersWithIncludes() => db.Users
        .Include(u => u.Institution)
        .Include(u => u.Coordinator);

    private async Task<ErrorOr<Success>> EnsureAdminAsync(int adminId, string action, CancellationToken ct)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", $"Only admins can {action}.");
        return Result.Success;
    }

    private static List<UserListResponse> ToUserListResponses(IEnumerable<User> users) =>
        users.Select(u => new UserListResponse(
            u.Id,
            u.Name,
            u.Email,
            u.Role.ToString(),
            u.Institution != null ? u.Institution.Name : null,
            u.InstitutionId,
            u.CoordinatorRequestStatus,
            u.IsOnboarded,
            u.Jmbag,
            u.Mentor,
            u.CoordinatorId,
            u.Coordinator != null ? u.Coordinator.Name : null))
            .ToList();

    #endregion
}
