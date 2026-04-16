using ErrorOr;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.CourseSlot;
using Loom.Application.DTOs.Institution;

namespace Loom.Application.Interfaces.Services;

public interface IInstitutionService
{
    Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<ForeignProgramResponse>>> GetForeignProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<StudyProgramResponse>>> GetStudyProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<ForeignCourseResponse>>> GetForeignCoursesAsync(Guid foreignProgramId, CancellationToken ct = default);
    Task<ErrorOr<List<AuthMeResponse>>> GetCoordinatorsAsync(CancellationToken ct = default);
}
