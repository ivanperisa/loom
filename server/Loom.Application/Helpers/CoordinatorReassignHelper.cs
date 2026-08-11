using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Helpers;

public static class CoordinatorReassignHelper
{
    public static async Task ReassignUnapprovedExchangesAsync(
        this IAppDbContext db, int studentId, int? coordinatorId, CancellationToken ct)
    {
        var exchanges = await db.Exchanges
            .Where(e => e.StudentId == studentId &&
                (e.LearningAgreement == null || e.LearningAgreement.Status != DocumentStatus.Approved))
            .ToListAsync(ct);

        foreach (var exchange in exchanges)
            exchange.CoordinatorId = coordinatorId;
    }
}
