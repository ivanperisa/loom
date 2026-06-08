using ErrorOr;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.Coordinator;
using Loom.Application.DTOs.Exchange;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class CoordinatorService(IAppDbContext db) : ICoordinatorService
{
    public async Task<ErrorOr<List<AuthMeResponse>>> GetCoordinatorsAsync(CancellationToken ct = default)
    {
        var coordinators = await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Coordinator || u.Role == UserRole.Admin)
            .OrderBy(u => u.Name)
            .ToListAsync(ct);
        return coordinators.Select(u => u.ToAuthMeResponse()).ToList();
    }

    public async Task<ErrorOr<List<CoordinatorStudentResponse>>> GetMyStudentsAsync(int coordinatorId, CancellationToken ct = default)
    {
        var coordinator = await db.Users.FindAsync([coordinatorId], ct);
        if (coordinator is null || !coordinator.CanActAsCoordinator())
            return Error.Forbidden("FORBIDDEN", "Only coordinators can view students.");

        var students = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .Where(u => u.CoordinatorId == coordinatorId && u.Role == UserRole.Student)
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

        return students
            .Select(u => new CoordinatorStudentResponse(
                u.Id, u.Name, u.Jmbag, u.Institution?.Name,
                u.ExternalId == u.Jmbag))
            .ToList();
    }

    public async Task<ErrorOr<CoordinatorStudentResponse>> CreatePlaceholderStudentAsync(int coordinatorId, CreatePlaceholderStudentRequest request, CancellationToken ct = default)
    {
        var coordinator = await db.Users.FindAsync([coordinatorId], ct);
        if (coordinator is null || !coordinator.CanActAsCoordinator())
            return Error.Forbidden("FORBIDDEN", "Only coordinators can create placeholder students.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Name is required.");

        if (string.IsNullOrWhiteSpace(request.Jmbag) || !System.Text.RegularExpressions.Regex.IsMatch(request.Jmbag, @"^\d{10}$"))
            return Error.Validation("INVALID_JMBAG", "JMBAG must be exactly 10 digits.");

        var jmbagTaken = await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag, ct);
        if (jmbagTaken) return Error.Conflict("JMBAG_TAKEN", "A student with this JMBAG already exists.");

        var institution = await db.Institutions.FindAsync([request.InstitutionId], ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
        if (institution.Type != InstitutionType.Home)
            return Error.Validation("INVALID_INSTITUTION", "Must select a home institution.");

        var placeholder = new User
        {
            ExternalId = request.Jmbag,
            Email = string.Empty,
            Name = request.Name.Trim(),
            Role = UserRole.Student,
            IsOnboarded = true,
            Jmbag = request.Jmbag,
            InstitutionId = request.InstitutionId,
            CoordinatorId = coordinatorId,
        };
        db.Users.Add(placeholder);
        await db.SaveChangesAsync(ct);

        return new CoordinatorStudentResponse(placeholder.Id, placeholder.Name, placeholder.Jmbag, institution.Name, true);
    }

    public async Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(int requesterId, CancellationToken ct = default)
    {
        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var query = db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.PartnerInstitution)
            .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
            .Include(e => e.LearningAgreement)
            .Include(e => e.Recognition);

        var exchanges = await query
            .Where(e => e.Student.CoordinatorId == requesterId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }
}
