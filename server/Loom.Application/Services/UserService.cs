using ErrorOr;
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
            var placeholder = await db.Users
                .FirstOrDefaultAsync(u => u.ExternalId == request.Jmbag && u.Jmbag == request.Jmbag, ct);

            if (placeholder is not null)
            {
                user.CoordinatorId ??= placeholder.CoordinatorId;

                var exchangesToTransfer = await db.Exchanges
                    .Where(e => e.StudentId == placeholder.Id)
                    .ToListAsync(ct);
                foreach (var ex in exchangesToTransfer)
                {
                    ex.StudentId = user.Id;
                    ex.Guid = Guid.NewGuid();
                }

                db.Users.Remove(placeholder);
                user.InstitutionId = request.InstitutionId;
                user.IsOnboarded = true;
                await db.SaveChangesAsync(ct);

                user.Jmbag = request.Jmbag;
                await db.SaveChangesAsync(ct);

                var merged = await UsersWithIncludes()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId, ct)
                    ?? throw new InvalidOperationException("User not found after merge.");
                return merged.ToAuthMeResponse();
            }

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
