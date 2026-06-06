using ErrorOr;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.Institution;
using Loom.Application.DTOs.LearningAgreement;

namespace Loom.Application.Interfaces.Services;

public interface IInstitutionService
{
    Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<HomeProgramResponse>>> GetHomeProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<PartnerProgramResponse>>> GetPartnerProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<PartnerCourseResponse>>> GetPartnerCoursesAsync(int partnerProgramId, CancellationToken ct = default);
    Task<ErrorOr<List<AuthMeResponse>>> GetCoordinatorsAsync(CancellationToken ct = default);

    Task<ErrorOr<List<PartnerInstitutionAdminResponse>>> GetPartnerInstitutionsAdminAsync(CancellationToken ct = default);
    Task<ErrorOr<PartnerInstitutionAdminResponse>> CreatePartnerInstitutionAsync(CreatePartnerInstitutionRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeletePartnerInstitutionAsync(int institutionId, CancellationToken ct = default);
    Task<ErrorOr<PartnerProgramAdminResponse>> CreatePartnerProgramAsync(int institutionId, CreatePartnerProgramRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeletePartnerProgramAsync(int programId, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseResponse>> CreatePartnerCourseAsync(int programId, CreatePartnerCourseRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeletePartnerCourseAsync(int courseId, CancellationToken ct = default);
}
