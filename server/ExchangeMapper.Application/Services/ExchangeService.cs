using ErrorOr;
using ExchangeMapper.Application.DTOs.CourseSlot;
using ExchangeMapper.Application.DTOs.Exchange;
using ExchangeMapper.Application.Interfaces;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Application.Mappers;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Application.Services;

public class ExchangeService(IAppDbContext db) : IExchangeService
{
    public async Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(Guid studentId, CreateExchangeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AcademicYear))
            return Error.Validation("INVALID_ACADEMIC_YEAR", "Academic year is required.");
        if (!Enum.TryParse<ExchangeSemester>(request.SemesterType, out var semesterType))
            return Error.Validation("INVALID_SEMESTER_TYPE", "Invalid semester type.");
        if (request.StudySemester < 1 || request.StudySemester > 4)
            return Error.Validation("INVALID_STUDY_SEMESTER", "Study semester must be between 1 and 4.");

        var existingExchange = await db.Exchanges
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudentId == studentId, ct);
        if (existingExchange is not null)
            return Error.Conflict("EXCHANGE_ALREADY_EXISTS", "Student already has an active exchange.");

        var studyProfile = await db.StudyProfiles.FindAsync([request.StudyProfileId], ct);
        if (studyProfile is null) return Error.NotFound("STUDY_PROFILE_NOT_FOUND", "Study profile not found.");

        var foreignProgram = await db.ForeignPrograms
            .AsNoTracking()
            .Include(p => p.Institution)
            .FirstOrDefaultAsync(p => p.Id == request.ForeignProgramId, ct);
        if (foreignProgram is null) return Error.NotFound("FOREIGN_PROGRAM_NOT_FOUND", "Foreign program not found.");

        if (request.CoordinatorId.HasValue)
        {
            var coordinator = await db.Users.FindAsync([request.CoordinatorId.Value], ct);
            if (coordinator is null || coordinator.Role != UserRole.Coordinator)
                return Error.NotFound("COORDINATOR_NOT_FOUND", "Coordinator not found.");
        }

        var exchange = new Exchange
        {
            StudentId = studentId,
            StudyProfileId = request.StudyProfileId,
            ForeignProgramId = request.ForeignProgramId,
            CoordinatorId = request.CoordinatorId,
            Mentor = request.Mentor,
            AcademicYear = request.AcademicYear,
            SemesterType = semesterType,
            StudySemester = request.StudySemester,
            Status = ExchangeStatus.Draft
        };

        db.Exchanges.Add(exchange);
        await db.SaveChangesAsync(ct);

        var saved = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.StudyProfile)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.Coordinator)
            .FirstOrDefaultAsync(e => e.Id == exchange.Id, ct)
            ?? throw new InvalidOperationException("Exchange not found after save.");
        return saved.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> GetExchangeAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.StudyProfile)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.Coordinator)
            .FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var canAccess = exchange.StudentId == requesterId
            || exchange.CoordinatorId == requesterId
            || requester.Role == UserRole.Admin;
        if (!canAccess) return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<ExchangeSummaryResponse>> GetMyExchangeSummaryAsync(Guid studentId, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.StudyProfile)
            .FirstOrDefaultAsync(e => e.StudentId == studentId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "No active exchange found.");
        return exchange.ToSummaryResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> UpdateExchangeStatusAsync(Guid exchangeId, Guid requesterId, UpdateExchangeStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ExchangeStatus>(request.Status, out var newStatus))
            return Error.Validation("INVALID_STATUS", "Invalid exchange status.");

        var exchange = await db.Exchanges.FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var isStudent = exchange.StudentId == requesterId;
        var isCoordinatorOrAdmin = exchange.CoordinatorId == requesterId || requester.Role == UserRole.Admin;

        if (!isStudent && !isCoordinatorOrAdmin)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        if (isStudent && newStatus != ExchangeStatus.Submitted && newStatus != ExchangeStatus.Draft)
            return Error.Forbidden("FORBIDDEN", "Students can only submit or revert to draft.");

        exchange.Status = newStatus;
        exchange.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var saved = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.StudyProfile)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.Coordinator)
            .FirstOrDefaultAsync(e => e.Id == exchangeId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    public async Task<ErrorOr<LearningAgreementResponse>> GetLearningAgreementAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.StudyProfile)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.Coordinator)
            .FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var canAccess = exchange.StudentId == requesterId
            || exchange.CoordinatorId == requesterId
            || requester.Role == UserRole.Admin;
        if (!canAccess) return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var slots = await db.CourseSlots
            .AsNoTracking()
            .Where(s => s.StudyProfileId == exchange.StudyProfileId)
            .OrderBy(s => s.Semester).ThenBy(s => s.ColStart)
            .ToListAsync(ct);

        var slotStates = await db.SlotStates
            .AsNoTracking()
            .Include(s => s.SlotMappings)
                .ThenInclude(m => m.ForeignCourse)
            .Where(s => s.ExchangeId == exchangeId)
            .ToListAsync(ct);

        return new LearningAgreementResponse(
            exchangeId,
            exchange.Status.ToString(),
            slots.Select(s => s.ToResponse()).ToList(),
            slotStates.Select(s => s.ToResponse()).ToList()
        );
    }

    public async Task<ErrorOr<LearningAgreementResponse>> SetSlotModeAsync(Guid exchangeId, Guid requesterId, SetSlotModeRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<SlotMode>(request.Mode, out var mode))
            return Error.Validation("INVALID_MODE", "Invalid slot mode.");

        var exchange = await db.Exchanges.FindAsync([exchangeId], ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        if (exchange.StudentId != requesterId && exchange.CoordinatorId != requesterId)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");
        if (exchange.Status is ExchangeStatus.Approved or ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_LOCKED", "Exchange cannot be modified in current status.");

        var slot = await db.CourseSlots.FindAsync([request.CourseSlotId], ct);
        if (slot is null || slot.StudyProfileId != exchange.StudyProfileId)
            return Error.NotFound("SLOT_NOT_FOUND", "Course slot not found for this profile.");

        var existing = await db.SlotStates
            .FirstOrDefaultAsync(s => s.ExchangeId == exchangeId && s.CourseSlotId == request.CourseSlotId, ct);
        if (existing is null)
        {
            var newState = new SlotState
            {
                ExchangeId = exchangeId,
                CourseSlotId = request.CourseSlotId,
                Mode = mode
            };
            db.SlotStates.Add(newState);
        }
        else
        {
            existing.Mode = mode;
        }

        await db.SaveChangesAsync(ct);
        return await GetLearningAgreementAsync(exchangeId, requesterId, ct);
    }

    public async Task<ErrorOr<LearningAgreementResponse>> AddSlotMappingAsync(Guid exchangeId, Guid requesterId, AddSlotMappingRequest request, CancellationToken ct = default)
    {
        if (request.AwardedEcts <= 0)
            return Error.Validation("INVALID_ECTS", "Awarded ECTS must be greater than 0.");

        var exchange = await db.Exchanges.FindAsync([exchangeId], ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        if (exchange.StudentId != requesterId && exchange.CoordinatorId != requesterId)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");
        if (exchange.Status is ExchangeStatus.Approved or ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_LOCKED", "Exchange cannot be modified in current status.");

        var slotState = await db.SlotStates
            .FirstOrDefaultAsync(s => s.ExchangeId == exchangeId && s.CourseSlotId == request.CourseSlotId, ct);
        if (slotState is null) return Error.NotFound("SLOT_STATE_NOT_FOUND", "Set a slot mode before mapping a course.");
        if (slotState.Mode != SlotMode.AtExchange) return Error.Validation("INVALID_MODE", "Can only map courses to slots marked as AtExchange.");

        var foreignCourse = await db.ForeignCourses.FindAsync([request.ForeignCourseId], ct);
        if (foreignCourse is null) return Error.NotFound("FOREIGN_COURSE_NOT_FOUND", "Foreign course not found.");

        var mapping = new SlotMapping
        {
            SlotStateId = slotState.Id,
            ForeignCourseId = request.ForeignCourseId,
            AwardedEcts = request.AwardedEcts
        };

        db.SlotMappings.Add(mapping);
        await db.SaveChangesAsync(ct);
        return await GetLearningAgreementAsync(exchangeId, requesterId, ct);
    }

    public async Task<ErrorOr<LearningAgreementResponse>> RemoveSlotMappingAsync(Guid exchangeId, Guid requesterId, RemoveSlotMappingRequest request, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges.FindAsync([exchangeId], ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        if (exchange.StudentId != requesterId && exchange.CoordinatorId != requesterId)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");
        if (exchange.Status is ExchangeStatus.Approved or ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_LOCKED", "Exchange cannot be modified in current status.");

        var mapping = await db.SlotMappings.FindAsync([request.SlotMappingId], ct);
        if (mapping is null) return Error.NotFound("MAPPING_NOT_FOUND", "Slot mapping not found.");

        var slotState = await db.SlotStates.FindAsync([mapping.SlotStateId], ct);
        if (slotState is null || slotState.ExchangeId != exchangeId)
            return Error.Forbidden("ACCESS_DENIED", "Mapping does not belong to this exchange.");

        db.SlotMappings.Remove(mapping);
        await db.SaveChangesAsync(ct);
        return await GetLearningAgreementAsync(exchangeId, requesterId, ct);
    }

    public async Task<ErrorOr<LearningAgreementResponse>> RemoveSlotStateAsync(Guid exchangeId, Guid requesterId, Guid courseSlotId, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges.FindAsync([exchangeId], ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        if (exchange.StudentId != requesterId && exchange.CoordinatorId != requesterId)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");
        if (exchange.Status is ExchangeStatus.Approved or ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_LOCKED", "Exchange cannot be modified in current status.");

        var slotState = await db.SlotStates
            .FirstOrDefaultAsync(s => s.ExchangeId == exchangeId && s.CourseSlotId == courseSlotId, ct);
        if (slotState is null) return await GetLearningAgreementAsync(exchangeId, requesterId, ct);

        var mappings = await db.SlotMappings.Where(m => m.SlotStateId == slotState.Id).ToListAsync(ct);
        db.SlotMappings.RemoveRange(mappings);
        db.SlotStates.Remove(slotState);
        await db.SaveChangesAsync(ct);
        return await GetLearningAgreementAsync(exchangeId, requesterId, ct);
    }

    public async Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(Guid coordinatorId, CancellationToken ct = default)
    {
        var exchanges = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.StudyProfile)
            .Where(e => e.CoordinatorId == coordinatorId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }
}
