using ErrorOr;
using Loom.Application.DTOs.Common;
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
    public async Task<ErrorOr<List<CoordinatorOptionResponse>>> GetCoordinatorsAsync(CancellationToken ct = default)
    {
        var coordinators = await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Coordinator || u.Role == UserRole.Admin)
            .OrderBy(u => u.Name)
            .Select(u => new CoordinatorOptionResponse(u.Id, u.Name))
            .ToListAsync(ct);
        return coordinators;
    }

    public async Task<ErrorOr<PagedResponse<CoordinatorStudentResponse>>> GetMyStudentsAsync(int coordinatorId, PagedRequest paging, CancellationToken ct = default)
    {
        var coordinator = await db.Users.FindAsync([coordinatorId], ct);
        if (coordinator is null || !coordinator.CanActAsCoordinator())
            return Error.Forbidden("FORBIDDEN", "Only coordinators can view students.");

        var query = db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .Where(u => u.Role == UserRole.Student &&
                (u.CoordinatorId == coordinatorId ||
                 u.StudentExchanges.Any(e => e.CoordinatorId == coordinatorId)));

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var term = $"%{paging.Search.Trim().ToLower()}%";
            query = query.Where(u =>
                EF.Functions.Like(u.Name.ToLower(), term) ||
                (u.Jmbag != null && EF.Functions.Like(u.Jmbag.ToLower(), term)));
        }

        var totalCount = await query.CountAsync(ct);

        var students = await query
            .OrderBy(u => u.Name)
            .Skip(paging.Skip)
            .Take(paging.SafePageSize)
            .ToListAsync(ct);

        var items = students
            .Select(u => new CoordinatorStudentResponse(
                u.Id, u.Name, u.Jmbag, u.Institution?.Name,
                u.ExternalId == u.Jmbag, u.InstitutionId,
                u.CoordinatorId == coordinatorId))
            .ToList();

        return new PagedResponse<CoordinatorStudentResponse>(items, paging.SafePage, paging.SafePageSize, totalCount);
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

        return new CoordinatorStudentResponse(placeholder.Id, placeholder.Name, placeholder.Jmbag, institution.Name, true, institution.Id, true);
    }

    public async Task<ErrorOr<CoordinatorStudentResponse>> UpdateStudentAsync(int coordinatorId, int studentId, UpdateStudentRequest request, CancellationToken ct = default)
    {
        var coordinator = await db.Users.FindAsync([coordinatorId], ct);
        if (coordinator is null || !coordinator.CanActAsCoordinator())
            return Error.Forbidden("FORBIDDEN", "Only coordinators can edit students.");

        var student = await db.Users.Include(u => u.Institution).FirstOrDefaultAsync(u => u.Id == studentId, ct);
        if (student is null || student.Role != UserRole.Student)
            return Error.NotFound("STUDENT_NOT_FOUND", "Student not found.");
        if (student.CoordinatorId != coordinatorId)
            return Error.Forbidden("FORBIDDEN", "You can only edit your own students.");
        if (student.ExternalId != student.Jmbag)
            return Error.Validation("NOT_A_PLACEHOLDER", "Only placeholder students can be edited here.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Name is required.");

        if (string.IsNullOrWhiteSpace(request.Jmbag) || !System.Text.RegularExpressions.Regex.IsMatch(request.Jmbag, @"^\d{10}$"))
            return Error.Validation("INVALID_JMBAG", "JMBAG must be exactly 10 digits.");

        var jmbagTaken = await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag && u.Id != studentId, ct);
        if (jmbagTaken) return Error.Conflict("JMBAG_TAKEN", "A student with this JMBAG already exists.");

        var institution = await db.Institutions.FindAsync([request.InstitutionId], ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
        if (institution.Type != InstitutionType.Home)
            return Error.Validation("INVALID_INSTITUTION", "Must select a home institution.");

        student.Name = request.Name.Trim();
        student.Jmbag = request.Jmbag;
        student.ExternalId = request.Jmbag;
        student.InstitutionId = request.InstitutionId;
        await db.SaveChangesAsync(ct);

        return new CoordinatorStudentResponse(student.Id, student.Name, student.Jmbag, institution.Name, true, institution.Id, true);
    }

    public async Task<ErrorOr<Deleted>> DeleteStudentAsync(int coordinatorId, int studentId, CancellationToken ct = default)
    {
        var coordinator = await db.Users.FindAsync([coordinatorId], ct);
        if (coordinator is null || !coordinator.CanActAsCoordinator())
            return Error.Forbidden("FORBIDDEN", "Only coordinators can delete students.");

        var student = await db.Users.FirstOrDefaultAsync(u => u.Id == studentId, ct);
        if (student is null || student.Role != UserRole.Student)
            return Error.NotFound("STUDENT_NOT_FOUND", "Student not found.");
        if (student.CoordinatorId != coordinatorId)
            return Error.Forbidden("FORBIDDEN", "You can only delete your own students.");
        if (student.ExternalId != student.Jmbag)
            return Error.Validation("NOT_A_PLACEHOLDER", "Only placeholder students can be deleted here.");

        var hasExchanges = await db.Exchanges.AnyAsync(e => e.StudentId == studentId, ct);
        if (hasExchanges)
            return Error.Conflict("HAS_EXCHANGES", "This student has exchanges. Delete them first.");

        db.Users.Remove(student);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
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

        // Keyed off the exchange's own coordinator, not the student's, so this matches
        // exactly what CheckExchangeAccessAsync will let the requester open. An approved
        // exchange stays listed for the coordinator who approved it even after the
        // student has been reassigned to someone else.
        var exchanges = await query
            .Where(e => e.CoordinatorId == requesterId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }
}
