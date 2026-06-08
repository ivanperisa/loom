using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.User;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class AdminService(IAppDbContext db) : IAdminService
{
    private IQueryable<User> UsersWithIncludes() => db.Users
        .Include(u => u.Institution)
        .Include(u => u.Coordinator);

    #region Users

    public async Task<ErrorOr<List<UserListResponse>>> GetAllUsersAsync(int adminId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can list users.");

        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .OrderBy(u => u.Name)
            .Select(u => new UserListResponse(
                u.Id,
                u.Name,
                u.Email,
                u.Role.ToString(),
                u.Institution != null ? u.Institution.Name : null,
                u.CoordinatorRequestStatus))
            .ToListAsync(ct);

        return users;
    }

    #endregion

    #region Coordinator role management

    public async Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(int adminId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can view coordinator requests.");

        var requests = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .Where(u => u.CoordinatorRequestStatus == "Pending")
            .Select(u => new CoordinatorRequestResponse(u.Id, u.Name, u.Email, u.Institution != null ? u.Institution.Name : null))
            .ToListAsync(ct);

        return requests;
    }

    public async Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can assign coordinator role.");

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
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can reject coordinator requests.");

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
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can remove coordinator role.");

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
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can view the coordinator whitelist.");

        var entries = await db.CoordinatorWhitelist
            .AsNoTracking()
            .OrderBy(e => e.Email)
            .Select(e => new CoordinatorWhitelistEntryResponse(e.Id, e.Email, e.CreatedAt))
            .ToListAsync(ct);

        return entries;
    }

    public async Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can manage the coordinator whitelist.");

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
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can manage the coordinator whitelist.");

        var entry = await db.CoordinatorWhitelist.FirstOrDefaultAsync(e => e.Email == email.ToLowerInvariant(), ct);
        if (entry is null) return Error.NotFound("EMAIL_NOT_FOUND", "Email not found on the coordinator whitelist.");

        db.CoordinatorWhitelist.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }

    #endregion
}
