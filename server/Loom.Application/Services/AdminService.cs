using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.Institution;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.DTOs.User;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class AdminService(IAppDbContext db) : IAdminService
{
    private IQueryable<User> UsersWithIncludes() => db.Users
        .Include(u => u.Institution)
        .Include(u => u.Coordinator);

    #region Users

    public async Task<ErrorOr<List<UserListResponse>>> GetAllUsersAsync(int adminId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can list users.");

        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .OrderBy(u => u.Name)
            .Select(u => new UserListResponse(
                u.Id,
                u.Name,
                u.Email,
                u.Role.ToString(),
                u.Institution != null ? u.Institution.Name : null,
                u.CoordinatorRequestStatus))
            .ToListAsync(ct);

        return users;
    }

    #endregion

    #region Coordinator role management

    public async Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(int adminId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can view coordinator requests.");

        var requests = await db.Users
            .AsNoTracking()
            .Include(u => u.Institution)
            .Where(u => u.CoordinatorRequestStatus == "Pending")
            .Select(u => new CoordinatorRequestResponse(u.Id, u.Name, u.Email, u.Institution != null ? u.Institution.Name : null))
            .ToListAsync(ct);

        return requests;
    }

    public async Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can assign coordinator role.");

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        target.Role = UserRole.Coordinator;
        target.CoordinatorRequestStatus = null;
        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> RejectCoordinatorRequestAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can reject coordinator requests.");

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (target.CoordinatorRequestStatus != "Pending")
            return Error.Validation("NO_PENDING_REQUEST", "User does not have a pending coordinator request.");

        target.CoordinatorRequestStatus = "Rejected";
        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToAuthMeResponse();
    }

    public async Task<ErrorOr<AuthMeResponse>> RemoveCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can remove coordinator role.");

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (target is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");
        if (target.Role != UserRole.Coordinator)
            return Error.Validation("NOT_COORDINATOR", "User is not a coordinator.");

        target.Role = UserRole.Student;
        target.CoordinatorRequestStatus = null;
        await db.SaveChangesAsync(ct);

        var saved = await UsersWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == targetUserId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToAuthMeResponse();
    }

    #endregion

    #region Coordinator whitelist

    public async Task<ErrorOr<List<CoordinatorWhitelistEntryResponse>>> GetCoordinatorWhitelistAsync(int adminId, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can view the coordinator whitelist.");

        var entries = await db.CoordinatorWhitelist
            .AsNoTracking()
            .OrderBy(e => e.Email)
            .Select(e => new CoordinatorWhitelistEntryResponse(e.Id, e.Email, e.CreatedAt))
            .ToListAsync(ct);

        return entries;
    }

    public async Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can manage the coordinator whitelist.");

        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation("INVALID_EMAIL", "Email is required.");

        var exists = await db.CoordinatorWhitelist.AnyAsync(e => e.Email == email.ToLowerInvariant(), ct);
        if (exists) return Error.Conflict("EMAIL_ALREADY_WHITELISTED", "This email is already on the coordinator whitelist.");

        var entry = new CoordinatorWhitelist { Email = email.Trim().ToLowerInvariant() };
        db.CoordinatorWhitelist.Add(entry);
        await db.SaveChangesAsync(ct);

        return new CoordinatorWhitelistEntryResponse(entry.Id, entry.Email, entry.CreatedAt);
    }

    public async Task<ErrorOr<Deleted>> RemoveFromCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default)
    {
        var admin = await db.Users.FindAsync([adminId], ct);
        if (admin is null || admin.Role != UserRole.Admin)
            return Error.Forbidden("FORBIDDEN", "Only admins can manage the coordinator whitelist.");

        var entry = await db.CoordinatorWhitelist.FirstOrDefaultAsync(e => e.Email == email.ToLowerInvariant(), ct);
        if (entry is null) return Error.NotFound("EMAIL_NOT_FOUND", "Email not found on the coordinator whitelist.");

        db.CoordinatorWhitelist.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Result.Deleted;
    }

    #endregion

    #region Partner institutions / programs / courses

    public async Task<ErrorOr<PartnerInstitutionAdminResponse>> CreatePartnerInstitutionAsync(CreatePartnerInstitutionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Institution name is required.");
        if (string.IsNullOrWhiteSpace(request.Country))
            return Error.Validation("INVALID_COUNTRY", "Country is required.");

        var institution = new Institution
        {
            Name = request.Name.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? request.Name.Trim() : request.NameEn.Trim(),
            Country = request.Country.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            ErasmusCode = string.IsNullOrWhiteSpace(request.ErasmusCode) ? null : request.ErasmusCode.Trim(),
            Type = InstitutionType.Partner,
        };
        db.Institutions.Add(institution);
        await db.SaveChangesAsync(ct);

        var saved = await db.Institutions
            .AsNoTracking()
            .Where(i => i.Id == institution.Id)
            .Include(i => i.PartnerPrograms)
                .ThenInclude(p => p.Courses)
            .FirstAsync(ct);
        return saved.ToAdminResponse();
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerInstitutionAsync(int institutionId, CancellationToken ct = default)
    {
        var institution = await db.Institutions
            .Include(i => i.PartnerPrograms)
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.Type == InstitutionType.Partner, ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");

        var hasExchanges = await db.Exchanges.AnyAsync(e => e.PartnerProgram.InstitutionId == institutionId, ct);
        if (hasExchanges) return Error.Conflict("HAS_EXCHANGES", "Cannot delete institution with active exchanges.");

        db.Institutions.Remove(institution);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<PartnerProgramAdminResponse>> CreatePartnerProgramAsync(int institutionId, CreatePartnerProgramRequest request, CancellationToken ct = default)
    {
        var institution = await db.Institutions.FindAsync([institutionId], ct);
        if (institution is null || institution.Type != InstitutionType.Partner)
            return Error.NotFound("INSTITUTION_NOT_FOUND", "Partner institution not found.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Program name is required.");
        if (!Enum.TryParse<StudyProgramLevel>(request.Level, out var level))
            return Error.Validation("INVALID_LEVEL", "Invalid study program level.");

        var program = new PartnerProgram
        {
            InstitutionId = institutionId,
            Name = request.Name.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? null : request.NameEn.Trim(),
            Level = level,
        };
        db.PartnerPrograms.Add(program);
        await db.SaveChangesAsync(ct);

        var saved = await db.PartnerPrograms
            .AsNoTracking()
            .Include(p => p.Courses)
            .FirstAsync(p => p.Id == program.Id, ct);
        return saved.ToAdminResponse();
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerProgramAsync(int programId, CancellationToken ct = default)
    {
        var program = await db.PartnerPrograms.FindAsync([programId], ct);
        if (program is null) return Error.NotFound("PROGRAM_NOT_FOUND", "Program not found.");

        var hasExchanges = await db.Exchanges.AnyAsync(e => e.PartnerProgramId == programId, ct);
        if (hasExchanges) return Error.Conflict("HAS_EXCHANGES", "Cannot delete program with active exchanges.");

        db.PartnerPrograms.Remove(program);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<PartnerCourseResponse>> CreatePartnerCourseAsync(int programId, CreatePartnerCourseRequest request, CancellationToken ct = default)
    {
        var program = await db.PartnerPrograms.FindAsync([programId], ct);
        if (program is null) return Error.NotFound("PROGRAM_NOT_FOUND", "Program not found.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return Error.Validation("INVALID_CODE", "Course code is required.");
        if (string.IsNullOrWhiteSpace(request.NameEn))
            return Error.Validation("INVALID_NAME", "Course name is required.");

        var course = new PartnerCourse
        {
            ProgramId = programId,
            Code = request.Code.Trim(),
            NameEn = request.NameEn.Trim(),
            NameHr = string.IsNullOrWhiteSpace(request.NameHr) ? null : request.NameHr.Trim(),
            Ects = request.Ects,
            LecturesH = request.LecturesH,
            AuditoryH = request.AuditoryH,
            LabH = request.LabH,
        };
        db.PartnerCourses.Add(course);
        await db.SaveChangesAsync(ct);
        return course.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return Error.NotFound("COURSE_NOT_FOUND", "Course not found.");

        db.PartnerCourses.Remove(course);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    #endregion
}
