using ErrorOr;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Helpers;

public static class CoordinatorReassignHelper
{
    public static async Task<ErrorOr<Success>> SetStudentCoordinatorAsync(
        this IAppDbContext db, User student, int? coordinatorId, CancellationToken ct)
    {
        if (student.CoordinatorId == coordinatorId) return Result.Success;

        if (coordinatorId is not null)
        {
            var coordinator = await db.Users.FindAsync([coordinatorId.Value], ct);
            if (coordinator is null || !coordinator.CanActAsCoordinator())
                return Error.NotFound("COORDINATOR_NOT_FOUND", "Coordinator not found.");
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
