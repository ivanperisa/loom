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
    IUserInstitutionRepository userInstitutionRepository,
    IUserRepository userRepository,
    ICourseRepository courseRepository) : IExchangeService
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

        var result = history.Select(h => h.ToResponse()).ToList();

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

    // --- Coordinator dashboard ---

    public async Task<ErrorOr<List<CoordinatorStudentSummaryResponse>>> GetCoordinatorStudentsAsync(Guid coordinatorId, CancellationToken ct = default)
    {
        var coordinator = await userRepository.GetByIdAsync(coordinatorId, ct);
        if (coordinator is null)
            return Error.NotFound("USER_NOT_FOUND", "Coordinator not found.");

        var exchanges = await exchangeRepository.GetByMentorNameAsync(coordinator.Name, ct);

        return exchanges.Select(e => new CoordinatorStudentSummaryResponse
        {
            ExchangeId = e.Id,
            StudentName = e.Student.Name,
            StudentEmail = e.Student.Email,
            StudentJmbag = e.Student.Jmbag,
            AcademicYear = e.AcademicYear,
            Semester = e.Semester.ToString(),
            Status = e.Status.ToString(),
            ForeignInstitutionName = e.ForeignInstitution.NameEn ?? e.ForeignInstitution.Name,
            ForeignInstitutionCountry = e.ForeignInstitution.Country,
            TotalCourses = e.ExchangeCourses.Count,
            PendingMappings = e.ExchangeCourses
                .SelectMany(c => c.CourseMappings)
                .Count(m => m.Status == CourseMappingStatus.Pending),
            ApprovedMappings = e.ExchangeCourses
                .SelectMany(c => c.CourseMappings)
                .Count(m => m.Status == CourseMappingStatus.Approved)
        }).ToList();
    }

    public async Task<ErrorOr<ExchangeResponse>> GetExchangeDetailsAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default)
    {
        var coordinator = await userRepository.GetByIdAsync(coordinatorId, ct);
        if (coordinator is null)
            return Error.NotFound("USER_NOT_FOUND", "Coordinator not found.");

        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (exchange.Mentor != coordinator.Name)
            return Error.Forbidden("FORBIDDEN", "You are not the mentor for this exchange.");

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> ApproveExchangeAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default)
    {
        var coordinator = await userRepository.GetByIdAsync(coordinatorId, ct);
        if (coordinator is null)
            return Error.NotFound("USER_NOT_FOUND", "Coordinator not found.");

        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (exchange.Mentor != coordinator.Name)
            return Error.Forbidden("FORBIDDEN", "You are not the mentor for this exchange.");

        if (exchange.Status != ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_NOT_SUBMITTABLE", "Exchange must be Submitted.");

        exchange.Status = ExchangeStatus.Approved;
        exchange.UpdatedAt = DateTime.UtcNow;

        foreach (var course in exchange.ExchangeCourses)
        foreach (var mapping in course.CourseMappings)
        {
            if (mapping.Status == CourseMappingStatus.Pending)
                mapping.Status = CourseMappingStatus.Approved;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return exchange.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> RejectExchangeAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default)
    {
        var coordinator = await userRepository.GetByIdAsync(coordinatorId, ct);
        if (coordinator is null)
            return Error.NotFound("USER_NOT_FOUND", "Coordinator not found.");

        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (exchange.Mentor != coordinator.Name)
            return Error.Forbidden("FORBIDDEN", "You are not the mentor for this exchange.");

        if (exchange.Status != ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_NOT_SUBMITTABLE", "Exchange must be Submitted.");

        exchange.Status = ExchangeStatus.Rejected;
        exchange.UpdatedAt = DateTime.UtcNow;

        foreach (var course in exchange.ExchangeCourses)
        foreach (var mapping in course.CourseMappings)
        {
            if (mapping.Status == CourseMappingStatus.Pending)
                mapping.Status = CourseMappingStatus.Rejected;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return exchange.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> ReturnExchangeAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default)
    {
        var coordinator = await userRepository.GetByIdAsync(coordinatorId, ct);
        if (coordinator is null)
            return Error.NotFound("USER_NOT_FOUND", "Coordinator not found.");

        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (exchange.Mentor != coordinator.Name)
            return Error.Forbidden("FORBIDDEN", "You are not the mentor for this exchange.");

        if (exchange.Status != ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_NOT_SUBMITTABLE", "Exchange must be Submitted.");

        exchange.Status = ExchangeStatus.Draft;
        exchange.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return exchange.ToResponse();
    }

    // --- Mapping board ---

    public async Task<ErrorOr<MappingBoardResponse>> GetMappingBoardAsync(Guid requesterId, Guid exchangeId, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var (isStudent, isCoordinator) = await CheckAccessAsync(requesterId, exchange, ct);
        if (!isStudent && !isCoordinator)
            return Error.Forbidden("FORBIDDEN", "Access denied.");

        var homeUi = await userInstitutionRepository.GetHomeByUserIdAsync(exchange.StudentId, ct);
        if (homeUi?.StudyProfileId is null)
            return Error.NotFound("STUDY_PROFILE_NOT_FOUND", "Student study profile not found.");

        var ferCourses = await courseRepository.GetByStudyProfileAsync(homeUi.StudyProfileId.Value, null, ct);

        var typeOrder = new[]
        {
            "Mandatory", "CoreElective", "ProfileElective", "FreeElective",
            "Transversal", "Seminar", "ResearchSeminar", "Project", "Thesis"
        };

        var ferGroups = typeOrder
            .Select(type => new FerCourseGroupResponse
            {
                Type = type,
                Courses = ferCourses
                    .Where(c => c.Type.ToString() == type)
                    .Select(c => new FerCourseResponse
                    {
                        Id = c.Id, Code = c.Code, Name = c.Name,
                        NameEn = c.NameEn, Ects = c.Ects, Type = c.Type.ToString()
                    }).ToList()
            })
            .Where(g => g.Courses.Count > 0)
            .ToList();

        var exchangeCourses = exchange.ExchangeCourses.Select(ec => new ExchangeCourseWithMappingsResponse
        {
            Id = ec.Id, Code = ec.Code, Name = ec.Name, NameEn = ec.NameEn,
            Ects = ec.Ects, Status = ec.Status.ToString(),
            Mappings = ec.CourseMappings.Select(m => new MappingRowResponse
            {
                Id = m.Id, FerCourseId = m.CourseId,
                FerCourseName = m.Course.Name, FerCourseCode = m.Course.Code,
                AwardedEcts = m.AwardedEcts, ConvertedGrade = m.ConvertedGrade,
                Status = m.Status.ToString(), CoordinatorNote = m.CoordinatorNote
            }).ToList()
        }).ToList();

        return new MappingBoardResponse { FerCourseGroups = ferGroups, ExchangeCourses = exchangeCourses };
    }

    public async Task<ErrorOr<MappingBoardResponse>> ProposeBoardMappingAsync(Guid requesterId, Guid exchangeId, ProposeBoardMappingRequest dto, CancellationToken ct = default)
    {
        var exchange = await exchangeRepository.GetByIdAsync(exchangeId, ct);
        if (exchange is null)
            return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var (isStudent, isCoordinator) = await CheckAccessAsync(requesterId, exchange, ct);
        if (!isStudent && !isCoordinator)
            return Error.Forbidden("FORBIDDEN", "Access denied.");

        if (isStudent && exchange.Status != ExchangeStatus.Draft && exchange.Status != ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_NOT_EDITABLE", "Cannot propose mappings in current status.");
        if (isCoordinator && exchange.Status != ExchangeStatus.Submitted)
            return Error.Conflict("EXCHANGE_NOT_EDITABLE", "Exchange must be Submitted for coordinator review.");

        foreach (var courseDto in dto.Courses)
        {
            var exchangeCourse = await exchangeCourseRepository.GetWithMappingsAsync(courseDto.ExchangeCourseId, ct);
            if (exchangeCourse is null || exchangeCourse.ExchangeId != exchangeId) continue;

            if (isCoordinator)
            {
                var pendingToDelete = exchangeCourse.CourseMappings
                    .Where(m => m.Status == CourseMappingStatus.Pending).ToList();
                foreach (var m in pendingToDelete)
                {
                    var snapshot = new MappingSnapshotResponse
                    {
                        CourseId = m.CourseId, CourseName = m.Course.Name,
                        CourseCode = m.Course.Code, AwardedEcts = m.AwardedEcts,
                        ConvertedGrade = m.ConvertedGrade, Status = m.Status.ToString(),
                        CoordinatorNote = m.CoordinatorNote
                    };
                    await mappingHistoryRepository.AddAsync(new MappingHistory
                    {
                        Id = Guid.CreateVersion7(),
                        CourseMappingId = m.Id,
                        ChangedBy = requesterId,
                        Snapshot = JsonSerializer.Serialize(snapshot)
                    }, ct);
                }
            }

            var pending = exchangeCourse.CourseMappings
                .Where(m => m.Status == CourseMappingStatus.Pending).ToList();
            await courseMappingRepository.DeleteRangeAsync(pending, ct);

            foreach (var entry in courseDto.Mappings)
            {
                await courseMappingRepository.AddAsync(new CourseMapping
                {
                    Id = Guid.CreateVersion7(),
                    ExchangeCourseId = courseDto.ExchangeCourseId,
                    CourseId = entry.FerCourseId,
                    Status = CourseMappingStatus.Pending,
                    AwardedEcts = entry.AwardedEcts,
                    ConvertedGrade = entry.ConvertedGrade,
                    CoordinatorNote = entry.CoordinatorNote
                }, ct);
            }
        }

        exchange.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        return await GetMappingBoardAsync(requesterId, exchangeId, ct);
    }

    private async Task<(bool isStudent, bool isCoordinator)> CheckAccessAsync(Guid requesterId, Exchange exchange, CancellationToken ct)
    {
        var requester = await userRepository.GetByIdAsync(requesterId, ct);
        if (requester is null) return (false, false);
        var isStudent = exchange.StudentId == requesterId;
        var isCoordinator = requester.Role == UserRole.Coordinator
            && exchange.Mentor == requester.Name;
        return (isStudent, isCoordinator);
    }
}
