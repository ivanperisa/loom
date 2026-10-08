using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Users;

/// <summary>The signed-in user's own account: profile, onboarding, coordinator request.</summary>
public sealed class AccountService(IAppDbContext db, ICurrentActor actor)
{
    public async Task<ErrorOr<AuthMeResponse>> GetAsync(int userId, CancellationToken ct)
    {
        var me = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(UserProjections.AuthMe).FirstOrDefaultAsync(ct);
        return me is null ? CommonErrors.UserNotFound : me;
    }

    public Task<ErrorOr<AuthMeResponse>> GetMeAsync(CancellationToken ct) => GetAsync(actor.UserId, ct);

    public async Task<ErrorOr<AuthMeResponse>> CompleteOnboardingAsync(CompleteOnboardingRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([actor.UserId], ct);
        if (user is null) return CommonErrors.UserNotFound;
        if (user.IsOnboarded) return UserErrors.AlreadyOnboarded;

        var institution = await HomeInstitutionAsync(request.InstitutionId, ct);
        if (institution.IsError) return institution.Errors;

        if (request.RequestCoordinatorRole)
        {
            user.CoordinatorRequestStatus = CoordinatorRequestStatus.Pending;
        }
        else if (!string.IsNullOrWhiteSpace(request.Jmbag))
        {
            var placeholder = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == request.Jmbag && u.Jmbag == request.Jmbag, ct);
            if (placeholder is not null)
            {
                // SECURITY (known, phase 4): knowing a JMBAG is not proof of owning it; this will require the access link.
                await TakeOverPlaceholderAsync(user, placeholder, request.InstitutionId, ct);
                return await GetMeAsync(ct);
            }

            if (await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag, ct)) return UserErrors.JmbagTaken;
            user.Jmbag = request.Jmbag;
        }

        user.InstitutionId = request.InstitutionId;
        user.IsOnboarded = true;
        await db.SaveChangesAsync(ct);
        return await GetMeAsync(ct);
    }

    public async Task<ErrorOr<AuthMeResponse>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([actor.UserId], ct);
        if (user is null) return CommonErrors.UserNotFound;

        var name = request.Name.NullIfBlank();
        if (name is null) return UserErrors.NameRequired;

        var institution = await HomeInstitutionAsync(request.InstitutionId, ct);
        if (institution.IsError) return institution.Errors;

        var jmbag = request.Jmbag.NullIfBlank();
        if (jmbag is not null && await db.Users.AnyAsync(u => u.Jmbag == jmbag && u.Id != user.Id, ct))
            return UserErrors.JmbagTaken;

        user.Jmbag = jmbag;
        user.Name = name;
        user.InstitutionId = request.InstitutionId;
        user.Mentor = request.Mentor.NullIfBlank();

        var assigned = await db.AssignCoordinatorAsync(user, request.CoordinatorId, ct);
        if (assigned.IsError) return assigned.Errors;

        await db.SaveChangesAsync(ct);
        return await GetMeAsync(ct);
    }

    public async Task<ErrorOr<AuthMeResponse>> RequestCoordinatorRoleAsync(CancellationToken ct)
    {
        var user = await db.Users.FindAsync([actor.UserId], ct);
        if (user is null) return CommonErrors.UserNotFound;
        if (user.Role != UserRole.Student) return UserErrors.NotAStudent;
        if (user.CoordinatorRequestStatus == CoordinatorRequestStatus.Pending) return UserErrors.RequestPending;

        user.CoordinatorRequestStatus = CoordinatorRequestStatus.Pending;
        await db.SaveChangesAsync(ct);
        return await GetMeAsync(ct);
    }

    /// <summary>Moves a placeholder's exchanges (and history) to the real account, then deletes the placeholder.</summary>
    private async Task TakeOverPlaceholderAsync(User user, User placeholder, int institutionId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        user.CoordinatorId = placeholder.CoordinatorId;
        foreach (var exchange in await db.Exchanges.Where(e => e.StudentId == placeholder.Id).ToListAsync(ct))
        {
            exchange.StudentId = user.Id;
            exchange.Guid = Guid.NewGuid();   // the old access link must stop working
        }
        // Snapshots reference their author without ON DELETE, so they have to move before the placeholder goes.
        foreach (var snapshot in await db.ExchangeSnapshots.Where(s => s.ChangedById == placeholder.Id).ToListAsync(ct))
            snapshot.ChangedById = user.Id;

        db.Users.Remove(placeholder);
        user.InstitutionId = institutionId;
        user.IsOnboarded = true;
        await db.SaveChangesAsync(ct);

        // Separate save: the JMBAG is unique and the placeholder must be gone first.
        user.Jmbag = placeholder.Jmbag;
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
    }

    private async Task<ErrorOr<Institution>> HomeInstitutionAsync(int institutionId, CancellationToken ct)
    {
        var institution = await db.Institutions.FindAsync([institutionId], ct);
        if (institution is null) return CommonErrors.InstitutionNotFound;
        if (institution.Type != InstitutionType.Home) return CommonErrors.NotAHomeInstitution;
        return institution;
    }
}
