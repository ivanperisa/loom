using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Querying;
using Loom.Application.Common.Security;
using Loom.Application.Features.Exchanges;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Coordination;

/// <summary>A coordinator's students, including placeholder students they create for people without an account.</summary>
public sealed class StudentService(IAppDbContext db, ICurrentActor actor)
{
    /// <summary>Each row carries the student's exchanges this coordinator coordinates (matching the filters), newest first.</summary>
    private static ListSpec<User, CoordinatorStudentResponse> ListFor(int coordinatorId, string? academicYear, string? partnerInstitution) => ListSpec.For<User>()
        .SearchIn(u => u.Name, u => u.Jmbag)
        .SortBy("name", u => u.Name, isDefault: true)
        .Project(u => new CoordinatorStudentResponse(
            u.Id, u.Name, u.Jmbag, u.Institution != null ? u.Institution.Name : null,
            u.Email == "", u.InstitutionId, u.CoordinatorId == coordinatorId,
            u.StudentExchanges
                .Where(e => e.CoordinatorId == coordinatorId
                    && (academicYear == null || e.AcademicYear == academicYear)
                    && (partnerInstitution == null || e.PartnerInstitution.Name == partnerInstitution))
                .OrderByDescending(e => e.CreatedAt)
                .AsQueryable()
                .Select(ExchangeProjections.Summary)
                .ToList()));

    /// <summary>Students assigned to the coordinator or with at least one exchange they coordinate.</summary>
    public async Task<ErrorOr<PagedResponse<CoordinatorStudentResponse>>> ListMineAsync(StudentListQuery query, CancellationToken ct)
    {
        var coordinator = await CurrentCoordinatorAsync("view students", ct);
        if (coordinator.IsError) return coordinator.Errors;
        var coordinatorId = coordinator.Value.Id;

        var academicYear = string.IsNullOrWhiteSpace(query.AcademicYear) ? null : query.AcademicYear;
        var partnerInstitution = string.IsNullOrWhiteSpace(query.PartnerInstitution) ? null : query.PartnerInstitution;
        var coordinated = db.Exchanges
            .Where(e => e.CoordinatorId == coordinatorId)
            .WhereIf(academicYear is not null, e => e.AcademicYear == academicYear)
            .WhereIf(partnerInstitution is not null, e => e.PartnerInstitution.Name == partnerInstitution);
        var filtered = academicYear is not null || partnerInstitution is not null;

        return await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Student &&
                (u.CoordinatorId == coordinatorId || u.StudentExchanges.Any(e => e.CoordinatorId == coordinatorId)))
            .WhereIf(filtered, u => coordinated.Any(e => e.StudentId == u.Id))
            .ToPageAsync(ListFor(coordinatorId, academicYear, partnerInstitution), query, ct);
    }

    /// <summary>The values the student list can be filtered by: years and partner institutions of the coordinator's exchanges.</summary>
    public async Task<ErrorOr<StudentFiltersResponse>> FiltersAsync(CancellationToken ct)
    {
        var coordinator = await CurrentCoordinatorAsync("view students", ct);
        if (coordinator.IsError) return coordinator.Errors;

        var exchanges = db.Exchanges.AsNoTracking().Where(e => e.CoordinatorId == coordinator.Value.Id);
        var years = await exchanges.Select(e => e.AcademicYear).Distinct().OrderByDescending(y => y).ToListAsync(ct);
        var institutions = await exchanges.Select(e => e.PartnerInstitution.Name).Distinct().OrderBy(n => n).ToListAsync(ct);
        return new StudentFiltersResponse(years, institutions);
    }

    public async Task<ErrorOr<CoordinatorStudentResponse>> CreatePlaceholderAsync(PlaceholderStudentRequest request, CancellationToken ct)
    {
        var coordinator = await CurrentCoordinatorAsync("create placeholder students", ct);
        if (coordinator.IsError) return coordinator.Errors;

        var validated = await ValidateAsync(request, studentId: null, ct);
        if (validated.IsError) return validated.Errors;
        var institution = validated.Value;

        var placeholder = new User
        {
            ExternalId = request.Jmbag,   // placeholders have no login; the JMBAG keeps the external id unique
            Email = string.Empty,
            Name = request.Name.Trim(),
            Role = UserRole.Student,
            IsOnboarded = true,
            Jmbag = request.Jmbag,
            InstitutionId = institution.Id,
            CoordinatorId = coordinator.Value.Id,
        };
        db.Users.Add(placeholder);
        await db.SaveChangesAsync(ct);

        return new CoordinatorStudentResponse(placeholder.Id, placeholder.Name, placeholder.Jmbag, institution.Name, true, institution.Id, true, []);
    }

    public async Task<ErrorOr<CoordinatorStudentResponse>> UpdatePlaceholderAsync(int studentId, PlaceholderStudentRequest request, CancellationToken ct)
    {
        var student = await OwnPlaceholderAsync(studentId, "edit", "edited", ct);
        if (student.IsError) return student.Errors;

        var validated = await ValidateAsync(request, studentId, ct);
        if (validated.IsError) return validated.Errors;
        var institution = validated.Value;

        student.Value.Name = request.Name.Trim();
        student.Value.Jmbag = request.Jmbag;
        student.Value.ExternalId = request.Jmbag;
        student.Value.InstitutionId = institution.Id;
        await db.SaveChangesAsync(ct);

        return new CoordinatorStudentResponse(studentId, student.Value.Name, request.Jmbag, institution.Name, true, institution.Id, true,
            await db.Exchanges.AsNoTracking().Where(e => e.StudentId == studentId && e.CoordinatorId == actor.UserId)
                .OrderByDescending(e => e.CreatedAt).Select(ExchangeProjections.Summary).ToListAsync(ct));
    }

    public async Task<ErrorOr<Deleted>> DeletePlaceholderAsync(int studentId, CancellationToken ct)
    {
        var student = await OwnPlaceholderAsync(studentId, "delete", "deleted", ct);
        if (student.IsError) return student.Errors;
        if (await db.Exchanges.AnyAsync(e => e.StudentId == studentId, ct)) return CoordinationErrors.HasExchanges;

        db.Users.Remove(student.Value);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    private async Task<ErrorOr<User>> CurrentCoordinatorAsync(string action, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([actor.UserId], ct);
        if (user is null || !user.CanActAsCoordinator()) return CoordinationErrors.NotACoordinator(action);
        return user;
    }

    private async Task<ErrorOr<User>> OwnPlaceholderAsync(int studentId, string verb, string pastTense, CancellationToken ct)
    {
        var coordinator = await CurrentCoordinatorAsync($"{verb} students", ct);
        if (coordinator.IsError) return coordinator.Errors;

        var student = await db.Users.FirstOrDefaultAsync(u => u.Id == studentId, ct);
        if (student is null || student.Role != UserRole.Student) return CoordinationErrors.StudentNotFound;
        if (!coordinator.Value.IsCoordinatorFor(student.CoordinatorId)) return CoordinationErrors.NotYourStudent(verb);
        if (!student.IsPlaceholder) return CoordinationErrors.NotAPlaceholder(pastTense);
        return student;
    }

    private async Task<ErrorOr<Institution>> ValidateAsync(PlaceholderStudentRequest request, int? studentId, CancellationToken ct)
    {
        if (request.Name.NullIfBlank() is null) return CoordinationErrors.NameRequired;
        if (!Text.IsValidJmbag(request.Jmbag)) return CommonErrors.InvalidJmbag;
        if (await db.Users.AnyAsync(u => u.Jmbag == request.Jmbag && u.Id != studentId, ct)) return CoordinationErrors.JmbagTaken;

        var institution = await db.Institutions.FindAsync([request.InstitutionId], ct);
        if (institution is null) return CommonErrors.InstitutionNotFound;
        if (institution.Type != InstitutionType.Home) return CommonErrors.NotAHomeInstitution;
        return institution;
    }
}
