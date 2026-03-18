using ErrorOr;
using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.DTOs.User;
using ExchangeMapper.Application.Interfaces;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Application.Mappers;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Application.Services;

public class UserService(IAppDbContext db) : IUserService, IUserSyncService
{
    public async Task<ErrorOr<AuthMeResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        return user.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> CompleteOnboardingAsync(Guid userId, CompleteOnboardingRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (user.IsOnboarded) return Error.Conflict("ALREADY_ONBOARDED", "User is already onboarded.");

        var institution = await db.Institutions.FindAsync([request.InstitutionId], ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
        if (!institution.IsHome) return Error.Validation("INVALID_INSTITUTION", "Must select a home institution.");

        var jmbagTaken = await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag, ct);
        if (jmbagTaken) return Error.Conflict("JMBAG_TAKEN", "This JMBAG is already in use.");

        user.Jmbag = request.Jmbag;
        user.InstitutionId = request.InstitutionId;
        user.IsOnboarded = true;

        await db.SaveChangesAsync(ct);

        var saved = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found after save.");
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(Guid adminId, Guid targetUserId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can assign coordinator role.");

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        target.Role = UserRole.Coordinator;
        await db.SaveChangesAsync(ct);

        var saved = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<User>> SyncUserAsync(string externalId, string email, string name, CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
        if (user is not null) return user;

        user = new User
        {
            ExternalId = externalId,
            Email = email,
            Name = name,
            Role = UserRole.Student,
            IsOnboarded = false
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
