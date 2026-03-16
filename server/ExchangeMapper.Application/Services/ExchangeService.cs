using System.Text.Json;
using ErrorOr;
using ExchangeMapper.Application.DTOs.Exchange;
using ExchangeMapper.Application.Interfaces;
using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Application.Mappers;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.Services;

public class ExchangeService(
    IUnitOfWork unitOfWork,
    IExchangeRepository exchangeRepository,
    IExchangeCourseRepository exchangeCourseRepository,
    ICourseMappingRepository courseMappingRepository,
    IMappingHistoryRepository mappingHistoryRepository,
    IUserInstitutionRepository userInstitutionRepository) : IExchangeService
{
    public async Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(Guid studentId, CreateExchangeRequest dto, CancellationToken ct = default)
    {
        var alreadyExists = await exchangeRepository.ExistsForStudentAsync(studentId, ct);
        if (alreadyExists)
        {
            return Error.Conflict("EXCHANGE_ALREADY_EXISTS", "Student already has an exchange.");
        }

        var homeUi = await userInstitutionRepository.GetHomeByUserIdAsync(studentId, ct);
        if (homeUi is null)
        {
            return Error.NotFound("HOME_INSTITUTION_NOT_FOUND", "Home institution not found for this student.");
        }

        if (!Enum.TryParse<ExchangeSemester>(dto.Semester, out var semester))
        {
            return Error.Validation("INVALID_SEMESTER", "Invalid semester value.");
        }

        var exchange = new Exchange
        {
            StudentId = studentId,
            UserInstitutionId = homeUi.Id,
            ForeignInstitutionId = dto.ForeignInstitutionId,
            AcademicYear = dto.AcademicYear,
            Semester = semester,
            Status = ExchangeStatus.Draft,
            DurationMonths = dto.DurationMonths,
            Mentor = dto.Mentor
        };

        await exchangeRepository.AddAsync(exchange, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await exchangeRepository.GetByStudentIdAsync(studentId, ct);
        return created!.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> GetMyExchangeAsync(Guid studentId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByStudentIdAsync(studentId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "No exchange found for this student.");
        }

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid studentId, Guid exchangeId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to delete this exchange.");
        }

        if (exchange.Status != ExchangeStatus.Draft)
        {
            return Error.Conflict("EXCHANGE_NOT_DELETABLE", "Only draft exchanges can be deleted.");
        }

        await exchangeRepository.DeleteAsync(exchange, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ExchangeCourseResponse>> AddCourseAsync(Guid studentId, Guid exchangeId, UpsertExchangeCourseRequest dto, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");
        }

        if (exchange.Status != ExchangeStatus.Draft)
        {
            return Error.Conflict("EXCHANGE_NOT_EDITABLE", "Cannot add courses to a submitted or finalized exchange.");
        }

        if (!Enum.TryParse<ExchangeCourseStatus>(dto.Status, out var courseStatus))
        {
            return Error.Validation("INVALID_STATUS", "Invalid course status.");
        }

        var course = new ExchangeCourse
        {
            ExchangeId = exchangeId,
            Code = dto.Code,
            Name = dto.Name,
            NameEn = dto.NameEn,
            NameHr = dto.NameHr,
            Ects = dto.Ects,
            Status = courseStatus,
            LecturesHours = dto.LecturesHours,
            AuditoryHours = dto.AuditoryHours,
            LabHours = dto.LabHours
        };

        await exchangeCourseRepository.AddAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await exchangeCourseRepository.GetWithMappingsAsync(course.Id, ct);
        return created!.ToResponse();
    }

    public async Task<ErrorOr<ExchangeCourseResponse>> UpdateCourseAsync(Guid studentId, Guid exchangeId, Guid courseId, UpsertExchangeCourseRequest dto, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");
        }

        if (exchange.Status != ExchangeStatus.Draft)
        {
            return Error.Conflict("EXCHANGE_NOT_EDITABLE", "Cannot update courses on a submitted or finalized exchange.");
        }

        var course = exchange.ExchangeCourses.FirstOrDefault(c => c.Id == courseId);
        if (course is null)
        {
            return Error.NotFound("COURSE_NOT_FOUND", "Exchange course not found.");
        }

        if (!Enum.TryParse<ExchangeCourseStatus>(dto.Status, out var courseStatus))
        {
            return Error.Validation("INVALID_STATUS", "Invalid course status.");
        }

        course.Code = dto.Code;
        course.Name = dto.Name;
        course.NameEn = dto.NameEn;
        course.NameHr = dto.NameHr;
        course.Ects = dto.Ects;
        course.Status = courseStatus;
        course.LecturesHours = dto.LecturesHours;
        course.AuditoryHours = dto.AuditoryHours;
        course.LabHours = dto.LabHours;

        await unitOfWork.SaveChangesAsync(ct);

        var updated = await exchangeCourseRepository.GetWithMappingsAsync(courseId, ct);
        return updated!.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> RemoveCourseAsync(Guid studentId, Guid exchangeId, Guid courseId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");
        }

        if (exchange.Status != ExchangeStatus.Draft)
        {
            return Error.Conflict("EXCHANGE_NOT_EDITABLE", "Cannot remove courses from a submitted or finalized exchange.");
        }

        var course = exchange.ExchangeCourses.FirstOrDefault(c => c.Id == courseId);
        if (course is null)
        {
            return Error.NotFound("COURSE_NOT_FOUND", "Exchange course not found.");
        }

        await exchangeCourseRepository.DeleteAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ExchangeCourseResponse>> UpdateGradesAsync(Guid studentId, Guid exchangeId, Guid courseId, UpdateGradesRequest dto, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");
        }

        var course = exchange.ExchangeCourses.FirstOrDefault(c => c.Id == courseId);
        if (course is null)
        {
            return Error.NotFound("COURSE_NOT_FOUND", "Exchange course not found.");
        }

        course.OriginalGrade = dto.OriginalGrade;
        course.EctsGrade = dto.EctsGrade;
        course.ExamDate = dto.ExamDate;

        await unitOfWork.SaveChangesAsync(ct);

        var updated = await exchangeCourseRepository.GetWithMappingsAsync(courseId, ct);
        return updated!.ToResponse();
    }

    public async Task<ErrorOr<ExchangeCourseResponse>> ProposeMappingAsync(Guid studentId, Guid exchangeId, Guid courseId, ProposeMappingRequest dto, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");
        }

        if (exchange.Status != ExchangeStatus.Draft && exchange.Status != ExchangeStatus.Submitted)
        {
            return Error.Conflict("EXCHANGE_NOT_EDITABLE", "Cannot propose mappings for an exchange that is not in Draft or Submitted status.");
        }

        var exchangeCourse = await exchangeCourseRepository.GetWithMappingsAsync(courseId, ct);
        if (exchangeCourse is null || exchangeCourse.ExchangeId != exchangeId)
        {
            return Error.NotFound("COURSE_NOT_FOUND", "Exchange course not found.");
        }

        var nonPendingCourseIds = exchangeCourse.CourseMappings
            .Where(m => m.Status != CourseMappingStatus.Pending)
            .Select(m => m.CourseId)
            .ToHashSet();

        foreach (var entry in dto.Mappings)
        {
            if (nonPendingCourseIds.Contains(entry.CourseId))
            {
                return Error.Conflict("MAPPING_CONFLICT", "Cannot propose a mapping for a course that already has an approved or rejected mapping.");
            }
        }

        var pendingMappings = exchangeCourse.CourseMappings
            .Where(m => m.Status == CourseMappingStatus.Pending)
            .ToList();

        await courseMappingRepository.DeleteRangeAsync(pendingMappings, ct);

        foreach (var entry in dto.Mappings)
        {
            var mapping = new CourseMapping
            {
                ExchangeCourseId = courseId,
                CourseId = entry.CourseId,
                Status = CourseMappingStatus.Pending,
                AwardedEcts = entry.AwardedEcts
            };
            await courseMappingRepository.AddAsync(mapping, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        var updated = await exchangeCourseRepository.GetWithMappingsAsync(courseId, ct);
        return updated!.ToResponse();
    }

    public async Task<ErrorOr<CourseMappingResponse>> ReviewMappingAsync(Guid coordinatorId, Guid exchangeId, Guid courseId, Guid mappingId, ReviewMappingRequest dto, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CourseMappingStatus>(dto.Status, out var newStatus))
        {
            return Error.Validation("INVALID_STATUS", "Invalid mapping status.");
        }

        var mapping = await courseMappingRepository.GetWithCourseAsync(mappingId, ct);
        if (mapping is null || mapping.ExchangeCourseId != courseId)
        {
            return Error.NotFound("MAPPING_NOT_FOUND", "Mapping not found.");
        }

        var exchangeCourse = await exchangeCourseRepository.GetWithMappingsAsync(courseId, ct);
        if (exchangeCourse is null || exchangeCourse.ExchangeId != exchangeId)
        {
            return Error.NotFound("COURSE_NOT_FOUND", "Exchange course not found.");
        }

        var snapshot = new MappingSnapshotResponse
        {
            CourseId = mapping.CourseId,
            CourseName = mapping.Course.Name,
            CourseCode = mapping.Course.Code,
            AwardedEcts = mapping.AwardedEcts,
            ConvertedGrade = mapping.ConvertedGrade,
            Status = mapping.Status.ToString(),
            CoordinatorNote = mapping.CoordinatorNote
        };

        var history = new MappingHistory
        {
            CourseMappingId = mapping.Id,
            ChangedBy = coordinatorId,
            Snapshot = JsonSerializer.Serialize(snapshot)
        };

        await mappingHistoryRepository.AddAsync(history, ct);

        mapping.Status = newStatus;
        mapping.CoordinatorNote = dto.CoordinatorNote;
        mapping.AwardedEcts = dto.AwardedEcts;
        mapping.ConvertedGrade = dto.ConvertedGrade;

        await unitOfWork.SaveChangesAsync(ct);

        return mapping.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> DeleteMappingAsync(Guid studentId, Guid exchangeId, Guid courseId, Guid mappingId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");
        }

        var exchangeCourse = exchange.ExchangeCourses.FirstOrDefault(c => c.Id == courseId);
        if (exchangeCourse is null)
        {
            return Error.NotFound("COURSE_NOT_FOUND", "Exchange course not found.");
        }

        var mapping = exchangeCourse.CourseMappings.FirstOrDefault(m => m.Id == mappingId);
        if (mapping is null)
        {
            return Error.NotFound("MAPPING_NOT_FOUND", "Mapping not found.");
        }

        await courseMappingRepository.DeleteAsync(mapping, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ExchangeResponse>> SubmitForReviewAsync(Guid studentId, Guid exchangeId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
        {
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        }

        if (exchange.StudentId != studentId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");
        }

        if (exchange.Status != ExchangeStatus.Draft)
        {
            return Error.Conflict("EXCHANGE_NOT_SUBMITTABLE", "Only draft exchanges can be submitted for review.");
        }

        exchange.Status = ExchangeStatus.Submitted;
        await unitOfWork.SaveChangesAsync(ct);

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> RetractAsync(Guid studentId, Guid exchangeId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (exchange.StudentId != studentId)
            return Error.Forbidden("FORBIDDEN", "You do not have permission to modify this exchange.");

        if (exchange.Status != ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_NOT_RETRACTABLE", "Only submitted exchanges can be retracted.");

        exchange.Status = ExchangeStatus.Draft;
        await unitOfWork.SaveChangesAsync(ct);

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<List<MappingHistoryResponse>>> GetMyHistoryAsync(Guid studentId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByStudentIdAsync(studentId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "No exchange found for this student.");

        return await GetExchangeHistoryAsync(exchange.Id, ct);
    }

    public async Task<ErrorOr<List<MappingHistoryResponse>>> GetExchangeHistoryAsync(Guid exchangeId, CancellationToken ct = default)
    {
        var history = await mappingHistoryRepository.GetByExchangeIdAsync(exchangeId, ct);

        var result = history.Select(h =>
        {
            MappingSnapshotResponse? snapshot = null;
            try
            {
                snapshot = JsonSerializer.Deserialize<MappingSnapshotResponse>(h.Snapshot);
            }
            catch
            {
                snapshot = new MappingSnapshotResponse { Status = "Unknown" };
            }

            return new MappingHistoryResponse
            {
                Id = h.Id,
                ChangedByName = h.ChangedByUser.Name,
                CreatedAt = h.CreatedAt,
                ExchangeCourseName = h.CourseMapping.ExchangeCourse.NameEn ?? h.CourseMapping.ExchangeCourse.Name,
                ExchangeCourseCode = h.CourseMapping.ExchangeCourse.Code,
                Snapshot = snapshot!
            };
        }).ToList();

        return result;
    }

    public async Task<ErrorOr<List<StudentExchangeSummaryResponse>>> GetStudentsWithExchangeAsync(CancellationToken ct = default)
    {
        var exchanges = await exchangeRepository.GetAllWithStudentAsync(ct);

        var result = exchanges.Select(e => new StudentExchangeSummaryResponse
        {
            ExchangeId = e.Id,
            StudentId = e.StudentId,
            StudentName = e.Student.Name,
            StudentEmail = e.Student.Email,
            StudentJmbag = e.Student.Jmbag,
            AcademicYear = e.AcademicYear,
            Semester = e.Semester.ToString(),
            Status = e.Status.ToString(),
            ForeignInstitutionName = e.ForeignInstitution.NameEn ?? e.ForeignInstitution.Name
        }).ToList();

        return result;
    }
}
