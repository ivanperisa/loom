using ErrorOr;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Querying;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Admin;

/// <summary>Students asking for coordinator access. Approving is a role change (<see cref="AdminUserService.SetRoleAsync"/>).</summary>
public sealed class CoordinatorRequestService(IAppDbContext db, AdminUserService users)
{
    private static readonly ListSpec<User, CoordinatorRequestResponse> List = ListSpec.For<User>()
        .SearchIn(u => u.Name, u => u.Email)
        .SortBy("name", u => u.Name, isDefault: true)
        .SortBy("email", u => u.Email)
        .Project(u => new CoordinatorRequestResponse(u.Id, u.Name, u.Email, u.Institution != null ? u.Institution.Name : null));

    public Task<PagedResponse<CoordinatorRequestResponse>> ListPendingAsync(ListQuery query, CancellationToken ct) =>
        db.Users
            .AsNoTracking()
            .Where(u => u.CoordinatorRequestStatus == CoordinatorRequestStatus.Pending && u.Role == UserRole.Student)
            .ToPageAsync(List, query, ct);

    /// <summary>Approving makes the student a coordinator (a role change); rejecting lets them ask again later.</summary>
    public async Task<ErrorOr<Success>> DecideAsync(int userId, string? status, CancellationToken ct)
    {
        var approve = string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);
        if (!approve && !string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)) return AdminErrors.InvalidDecision;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return CommonErrors.UserNotFound;
        if (user.CoordinatorRequestStatus != CoordinatorRequestStatus.Pending) return AdminErrors.NoPendingRequest;

        if (approve)
        {
            var promoted = await users.SetRoleAsync(userId, UserRole.Coordinator, ct);
            return promoted.IsError ? promoted.Errors : Result.Success;
        }

        user.CoordinatorRequestStatus = CoordinatorRequestStatus.Rejected;
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}
