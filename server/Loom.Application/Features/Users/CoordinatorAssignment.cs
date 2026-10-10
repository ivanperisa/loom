using ErrorOr;
using Loom.Application.Common.Errors;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Users;

public static class CoordinatorAssignment
{
    /// <summary>
    /// Assigns a student's coordinator and moves their not-yet-approved exchanges along.
    /// Approved exchanges keep the coordinator who approved them.
    /// </summary>
    public static async Task<ErrorOr<Success>> AssignCoordinatorAsync(this IAppDbContext db, User student, int? coordinatorId, CancellationToken ct)
    {
        if (student.CoordinatorId == coordinatorId) return Result.Success;

        if (coordinatorId is not null)
        {
            var coordinator = await db.Users.FindAsync([coordinatorId.Value], ct);
            if (coordinator is null || !coordinator.CanActAsCoordinator()) return CommonErrors.CoordinatorNotFound;
        }

        student.CoordinatorId = coordinatorId;

        var exchanges = await db.Exchanges
            .Where(e => e.StudentId == student.Id &&
                (e.LearningAgreement == null || e.LearningAgreement.Status != DocumentStatus.Approved))
            .ToListAsync(ct);
        foreach (var exchange in exchanges)
        {
            exchange.CoordinatorId = coordinatorId;
            exchange.CoordinatorMessage = null;
        }
        return Result.Success;
    }
}
