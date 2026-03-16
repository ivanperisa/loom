using ErrorOr;
using ExchangeMapper.Application.DTOs.Course;
using ExchangeMapper.Application.DTOs.Institution;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IInstitutionService
{
    Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<InstitutionResponse>>> GetForeignInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<StudyProgramResponse>>> GetProgramsByInstitutionAsync(Guid institutionId, CancellationToken ct = default);
    Task<ErrorOr<List<StudyProfileResponse>>> GetProfilesByProgramAsync(Guid institutionId, Guid programId, CancellationToken ct = default);
    Task<ErrorOr<List<CourseResponse>>> GetAvailableCoursesAsync(Guid studentId, string? query, CancellationToken ct = default);
}
