using ErrorOr;
using Loom.Application.DTOs.Exchange;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class ExchangeService(IAppDbContext db) : IExchangeService
{
    public async Task<ErrorOr<ExchangeResponse>> GetExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (exchange.StudentId != requesterId && !requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> GetPublicExchangeAsync(Guid exchangeGuid, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (!string.IsNullOrEmpty(exchange.Student.Email))
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<int>> ResolveGuestStudentIdAsync(Guid exchangeGuid, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (!string.IsNullOrEmpty(exchange.Student.Email))
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return exchange.StudentId;
    }

    public async Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyExchangesAsync(int studentId, CancellationToken ct = default)
    {
        var exchanges = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.PartnerInstitution)
            .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
            .Include(e => e.LearningAgreement)
            .Include(e => e.Recognition)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }

    public async Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(int requesterId, CreateExchangeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AcademicYear))
            return Error.Validation("INVALID_ACADEMIC_YEAR", "Academic year is required.");
        if (!Enum.TryParse<ExchangeSemester>(request.SemesterType, out var semesterType))
            return Error.Validation("INVALID_SEMESTER_TYPE", "Invalid semester type.");
        if (request.StudySemesters is not { Count: > 0 } || request.StudySemesters.Any(s => s < 1 || s > 10))
            return Error.Validation("INVALID_STUDY_SEMESTER", "Study semesters must be between 1 and 10.");

        var actualStudentId = request.TargetStudentId ?? requesterId;

        if (actualStudentId != requesterId)
        {
            var requester = await db.Users.FindAsync([requesterId], ct);
            if (requester is null || !requester.CanActAsCoordinator())
                return Error.Forbidden("FORBIDDEN", "Only coordinators can create exchanges for other students.");

            var targetStudent = await db.Users.FindAsync([actualStudentId], ct);
            if (targetStudent is null) return Error.NotFound("USER_NOT_FOUND", "Target student not found.");

            if (targetStudent.CoordinatorId != requesterId)
                return Error.Forbidden("FORBIDDEN", "You are not the coordinator for this student.");
        }

        var studentId = actualStudentId;
        var student = await db.Users.FindAsync([studentId], ct);
        if (student is null) return Error.NotFound("USER_NOT_FOUND", "Student not found.");

        var homeProfile = await db.HomeProfiles.FindAsync([request.HomeProfileId], ct);
        if (homeProfile is null) return Error.NotFound("HOME_PROFILE_NOT_FOUND", "Home profile not found.");

        var partnerInstitution = await db.Institutions
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.PartnerInstitutionId && i.Type == InstitutionType.Partner, ct);
        if (partnerInstitution is null) return Error.NotFound("PARTNER_INSTITUTION_NOT_FOUND", "Partner institution not found.");

        if (request.CoordinatorId.HasValue && student.CoordinatorId != request.CoordinatorId)
            student.CoordinatorId = request.CoordinatorId.Value;

        var exchange = new Exchange
        {
            StudentId = studentId,
            CoordinatorId = request.CoordinatorId ?? student.CoordinatorId,
            HomeProfileId = request.HomeProfileId,
            PartnerInstitutionId = partnerInstitution.Id,
            AcademicYear = request.AcademicYear,
            SemesterType = semesterType,
            StudySemesters = request.StudySemesters,
        };
        db.Exchanges.Add(exchange);
        await db.SaveChangesAsync(ct);

        db.LearningAgreements.Add(new LearningAgreement { ExchangeId = exchange.Id, Status = DocumentStatus.Draft });
        await db.SaveChangesAsync(ct);

        var saved = await ExchangeWithIncludes()
            .FirstOrDefaultAsync(e => e.Id == exchange.Id, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await db.Exchanges
            .Include(e => e.Student)
            .FirstOrDefaultAsync(e => e.Id == exchangeId, ct);

        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (exchange.StudentId != requesterId &&
            exchange.Student.CoordinatorId != requesterId)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var la = await db.LearningAgreements
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.ExchangeId == exchangeId, ct);

        var recognition = await db.Recognitions
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);

        if (la?.Status != DocumentStatus.Draft || recognition?.Status != DocumentStatus.Draft)
            return Error.Conflict("NOT_DRAFT", "Only draft exchanges can be deleted.");

        db.ExchangeSnapshots.RemoveRange(
            await db.ExchangeSnapshots.Where(s => s.ExchangeId == exchangeId).ToListAsync(ct));

        if (la is not null)
        {
            db.LearningAgreementEntries.RemoveRange(la.Entries);
            db.LearningAgreements.Remove(la);
        }

        if (recognition is not null)
        {
            db.RecognitionEntries.RemoveRange(recognition.Entries);
            db.Recognitions.Remove(recognition);
        }

        db.Exchanges.Remove(exchange);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeGuid, int requesterId, string? message, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (!requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Only coordinators can update the message.");

        exchange.CoordinatorMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        exchange.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var saved = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    #region Private Methods

    private IQueryable<Exchange> ExchangeWithIncludes() => db.Exchanges
        .AsNoTracking()
        .Include(e => e.Student)
        .Include(e => e.Coordinator)
        .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
        .Include(e => e.PartnerInstitution)
        .Include(e => e.LearningAgreement);

    #endregion
}
