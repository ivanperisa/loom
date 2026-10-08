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
            if (!Text.IsValidJmbag(request.Jmbag)) return CommonErrors.InvalidJmbag;
            var taken = await JmbagInUseAsync(request.Jmbag, user.Id, ct);
            if (taken.IsError) return taken.Errors;
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
        if (jmbag is not null && !Text.IsValidJmbag(jmbag)) return CommonErrors.InvalidJmbag;
        if (jmbag is not null)
        {
            var taken = await JmbagInUseAsync(jmbag, user.Id, ct);
            if (taken.IsError) return taken.Errors;
        }

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

    /// <summary>
    /// A JMBAG belongs to one user. If a coordinator already created a placeholder with it, the student has to
    /// open the access link they were sent; typing the JMBAG alone gives no access to anything.
    /// </summary>
    private async Task<ErrorOr<Success>> JmbagInUseAsync(string jmbag, int userId, CancellationToken ct)
    {
        var owner = await db.Users.Where(u => u.Jmbag == jmbag && u.Id != userId).Select(u => new { u.Email }).FirstOrDefaultAsync(ct);
        if (owner is null) return Result.Success;
        return owner.Email.Length == 0 ? UserErrors.JmbagReserved : UserErrors.JmbagTaken;
    }

    private async Task<ErrorOr<Institution>> HomeInstitutionAsync(int institutionId, CancellationToken ct)
    {
        var institution = await db.Institutions.FindAsync([institutionId], ct);
        if (institution is null) return CommonErrors.InstitutionNotFound;
        if (institution.Type != InstitutionType.Home) return CommonErrors.NotAHomeInstitution;
        return institution;
    }
}
