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

public class UserService(IAppDbContext db) : IUserService, IUserSyncService
{
    private IQueryable<User> UsersWithIncludes() => db.Users
        .Include(u => u.Institution)
        .Include(u => u.Coordinator);

    public async Task<ErrorOr<AuthMeResponse>> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        return user.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> CompleteOnboardingAsync(int userId, CompleteOnboardingRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (user.IsOnboarded) return Error.Conflict("ALREADY_ONBOARDED", "User is already onboarded.");

        var institution = await db.Institutions.FindAsync([request.InstitutionId], ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
        if (institution.Type != InstitutionType.Home)
            return Error.Validation("INVALID_INSTITUTION", "Must select a home institution.");

        if (request.RequestCoordinatorRole)
        {
            user.CoordinatorRequestStatus = "Pending";
        }
        else if (!string.IsNullOrWhiteSpace(request.Jmbag))
        {
            var jmbagTaken = await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag, ct);
            if (jmbagTaken) return Error.Conflict("JMBAG_TAKEN", "This JMBAG is already in use.");
            user.Jmbag = request.Jmbag;
        }

        user.InstitutionId = request.InstitutionId;
        user.IsOnboarded = true;

        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found after save.");
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Name is required.");

        var institution = await db.Institutions.FindAsync([request.InstitutionId], ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
        if (institution.Type != InstitutionType.Home)
            return Error.Validation("INVALID_INSTITUTION", "Must select a home institution.");

        if (!string.IsNullOrWhiteSpace(request.Jmbag))
        {
            var jmbagTaken = await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag && u.Id != userId, ct);
            if (jmbagTaken) return Error.Conflict("JMBAG_TAKEN", "This JMBAG is already in use.");
            user.Jmbag = request.Jmbag;
        }
        else
        {
            user.Jmbag = null;
        }

        user.Name = request.Name.Trim();
        user.InstitutionId = request.InstitutionId;
        user.Mentor = string.IsNullOrWhiteSpace(request.Mentor) ? null : request.Mentor.Trim();

        if (request.CoordinatorId.HasValue)
        {
            var coordinator = await db.Users.FindAsync([request.CoordinatorId.Value], ct);
            if (coordinator is null || !coordinator.CanActAsCoordinator())
                return Error.NotFound("COORDINATOR_NOT_FOUND", "Coordinator not found.");
        }
        user.CoordinatorId = request.CoordinatorId;

        var activeExchanges = await db.Exchanges
            .Where(e => e.StudentId == user.Id &&
                e.LearningAgreement != null &&
                e.LearningAgreement.Status != DocumentStatus.Approved &&
                e.LearningAgreement.Status != DocumentStatus.Rejected)
            .ToListAsync(ct);
        foreach (var exchange in activeExchanges)
            exchange.CoordinatorId = request.CoordinatorId;

        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found after save.");
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> RequestCoordinatorRoleAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (user.Role != UserRole.Student)
            return Error.Conflict("ALREADY_COORDINATOR", "Only students can request coordinator access.");
        if (user.CoordinatorRequestStatus == "Pending")
            return Error.Conflict("REQUEST_ALREADY_PENDING", "A coordinator request is already pending.");

        user.CoordinatorRequestStatus = "Pending";
        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found after save.");
        return saved.ToAuthMeResponse();
    }

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

    public async Task<ErrorOr<User>> SyncUserAsync(string externalId, string email, string name, CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
        if (user is not null) return user;

        var isWhitelisted = await db.CoordinatorWhitelist
            .AnyAsync(e => e.Email == email.ToLowerInvariant(), ct);

        user = new User
        {
            ExternalId = externalId,
            Email = email,
            Name = name,
            Role = isWhitelisted ? UserRole.Coordinator : UserRole.Student,
            IsOnboarded = false
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
