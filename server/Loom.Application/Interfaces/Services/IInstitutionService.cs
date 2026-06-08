using ErrorOr;
using Loom.Application.DTOs.Institution;
using Loom.Application.DTOs.LearningAgreement;

namespace Loom.Application.Interfaces.Services;

public interface IInstitutionService
{
    Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<HomeProgramResponse>>> GetHomeProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<PartnerInstitutionAdminResponse>>> GetPartnerInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<PartnerInstitutionAdminResponse>> CreatePartnerInstitutionAsync(CreatePartnerInstitutionRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeletePartnerInstitutionAsync(int institutionId, CancellationToken ct = default);
    Task<ErrorOr<List<PartnerCourseResponse>>> GetPartnerCoursesByInstitutionAsync(int institutionId, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseResponse>> CreatePartnerCourseByInstitutionAsync(int institutionId, CreatePartnerCourseRequest request, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseResponse>> UpdatePartnerCourseAsync(int courseId, UpdatePartnerCourseRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeletePartnerCourseAsync(int courseId, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseResponse>> MergePartnerCoursesAsync(MergePartnerCoursesRequest request, CancellationToken ct = default);
}
