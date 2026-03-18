using ErrorOr;
using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.DTOs.CourseSlot;
using ExchangeMapper.Application.DTOs.Institution;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IInstitutionService
{
    Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<ForeignProgramResponse>>> GetForeignProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<StudyProgramResponse>>> GetStudyProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<ForeignCourseResponse>>> GetForeignCoursesAsync(Guid foreignProgramId, CancellationToken ct = default);
    Task<ErrorOr<List<AuthMeResponse>>> GetCoordinatorsAsync(CancellationToken ct = default);
}
