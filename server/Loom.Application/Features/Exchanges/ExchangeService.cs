using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Security;
using Loom.Application.Features.Users;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Exchanges;

public sealed class ExchangeService(IAppDbContext db, ICurrentActor actor, ExchangeAccess access)
{
    public async Task<ErrorOr<ExchangeResponse>> GetAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        return await GetByIdAsync(context.Value.ExchangeId, ct);
    }

    public Task<List<ExchangeSummaryResponse>> ListMineAsync(CancellationToken ct) =>
        db.Exchanges
            .AsNoTracking()
            .Where(e => e.StudentId == actor.UserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(ExchangeProjections.Summary)
            .ToListAsync(ct);

    /// <summary>Students create their own exchanges; coordinators can create one for a student they coordinate.</summary>
    public async Task<ErrorOr<ExchangeResponse>> CreateAsync(CreateExchangeRequest request, CancellationToken ct)
    {
        var period = ValidatePeriod(request.AcademicYear, request.SemesterType, request.StudySemesters);
        if (period.IsError) return period.Errors;

        var studentId = request.TargetStudentId ?? actor.UserId;
        if (studentId != actor.UserId)
        {
            var requester = await db.Users.FindAsync([actor.UserId], ct);
            if (requester is null || !requester.CanActAsCoordinator()) return ExchangeErrors.OnlyCoordinatorsForOthers;

            var target = await db.Users.FindAsync([studentId], ct);
            if (target is null) return ExchangeErrors.TargetStudentNotFound;
            if (!requester.IsCoordinatorFor(target.CoordinatorId)) return ExchangeErrors.NotYourStudent;
        }

        var student = await db.Users.FindAsync([studentId], ct);
        if (student is null) return ExchangeErrors.StudentNotFound;

        if (await db.HomeProfiles.FindAsync([request.HomeProfileId], ct) is null) return ExchangeErrors.HomeProfileNotFound;
        var partnerExists = await db.Institutions.AnyAsync(i => i.Id == request.PartnerInstitutionId && i.Type == InstitutionType.Partner, ct);
        if (!partnerExists) return ExchangeErrors.PartnerInstitutionNotFound;

        if (request.CoordinatorId.HasValue)
        {
            var assigned = await db.AssignCoordinatorAsync(student, request.CoordinatorId.Value, ct);
            if (assigned.IsError) return assigned.Errors;
        }
        if (request.Mentor.NullIfBlank() is { } mentor) student.Mentor = mentor;

        var exchange = new Exchange
        {
            StudentId = studentId,
            CoordinatorId = student.CoordinatorId,
            HomeProfileId = request.HomeProfileId,
            PartnerInstitutionId = request.PartnerInstitutionId,
            AcademicYear = request.AcademicYear,
            SemesterType = period.Value,
            StudySemesters = request.StudySemesters,
            LearningAgreement = new LearningAgreement { Status = DocumentStatus.Draft },
        };
        db.Exchanges.Add(exchange);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(exchange.Id, ct);
    }

    /// <summary>Guests (access link) can edit the period and mentor, but not change the coordinator.</summary>
    public async Task<ErrorOr<ExchangeResponse>> UpdateAsync(Guid exchangeGuid, UpdateExchangeRequest request, CancellationToken ct)
    {
        var period = ValidatePeriod(request.AcademicYear, request.SemesterType, request.StudySemesters);
        if (period.IsError) return period.Errors;
        var semesterType = period.Value;

        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = await db.Exchanges.SingleAsync(e => e.Id == context.Value.ExchangeId, ct);

        if (exchange.SemesterType != semesterType && semesterType != ExchangeSemester.Both)
        {
            // Winter covers odd study semesters, summer even ones.
            var targetParity = semesterType == ExchangeSemester.Winter ? 1 : 0;
            var hasEntriesOutside = await db.LearningAgreementEntries
                .AnyAsync(e => e.LearningAgreement.ExchangeId == exchange.Id && !e.IsDeleted && e.PartnerCourseId != null
                    && e.HomeSlot.Semester % 2 != targetParity, ct);
            if (hasEntriesOutside) return ExchangeErrors.SemesterHasEntries;
        }

        var student = await db.Users.FindAsync([exchange.StudentId], ct);
        if (student is null) return ExchangeErrors.StudentNotFound;
        student.Mentor = request.Mentor.NullIfBlank();

        if (!actor.IsGuest)
        {
            var assigned = await db.AssignCoordinatorAsync(student, request.CoordinatorId, ct);
            if (assigned.IsError) return assigned.Errors;
        }

        exchange.AcademicYear = request.AcademicYear;
        exchange.SemesterType = semesterType;
        exchange.StudySemesters = request.StudySemesters;
        exchange.EwpLink = request.EwpLink.NullIfBlank();
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(exchange.Id, ct);
    }

    /// <summary>Only while both documents are drafts. Documents, entries and history go with it (database cascades).</summary>
    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchangeId = context.Value.ExchangeId;

        var locked = await db.Exchanges
            .Where(e => e.Id == exchangeId)
            .AnyAsync(e => (e.LearningAgreement != null && e.LearningAgreement.Status != DocumentStatus.Draft)
                || (e.Recognition != null && e.Recognition.Status != DocumentStatus.Draft), ct);
        if (locked) return ExchangeErrors.NotDraft;

        db.Exchanges.Remove(await db.Exchanges.SingleAsync(e => e.Id == exchangeId, ct));
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeGuid, string? message, CancellationToken ct)
    {
        var exchange = await db.Exchanges.FirstOrDefaultAsync(e => e.Guid == exchangeGuid, ct);
        if (exchange is null) return CommonErrors.ExchangeNotFound;
        if (exchange.CoordinatorId != actor.UserId) return ExchangeErrors.OnlyCoordinatorMessage;

        exchange.CoordinatorMessage = message.NullIfBlank();
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(exchange.Id, ct);
    }

    private async Task<ErrorOr<ExchangeResponse>> GetByIdAsync(int exchangeId, CancellationToken ct)
    {
        var exchange = await db.Exchanges.AsNoTracking().Where(e => e.Id == exchangeId).Select(ExchangeProjections.Detail).FirstOrDefaultAsync(ct);
        return exchange is null ? CommonErrors.ExchangeNotFound : exchange;
    }

    private static ErrorOr<ExchangeSemester> ValidatePeriod(string academicYear, string semesterType, List<int>? studySemesters)
    {
        if (string.IsNullOrWhiteSpace(academicYear)) return ExchangeErrors.AcademicYearRequired;
        if (!Enum.TryParse<ExchangeSemester>(semesterType, out var semester)) return ExchangeErrors.InvalidSemesterType;
        if (studySemesters is not { Count: > 0 } || studySemesters.Any(s => s < 1 || s > 10)) return ExchangeErrors.InvalidStudySemesters;
        return semester;
    }
}
