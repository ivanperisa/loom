using ErrorOr;
using Loom.Application.DTOs.Common;
using Loom.Application.DTOs.Institution;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Domain.Enums;

namespace Loom.Application.Interfaces.Services;

public interface IInstitutionService
{
    Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<HomeProgramResponse>>> GetHomeProgramsAsync(CancellationToken ct = default);
    Task<ErrorOr<PagedResponse<PartnerInstitutionAdminResponse>>> GetPartnerInstitutionsAsync(bool includeDeleted, PagedRequest paging, string? country = null, string? sortBy = null, CancellationToken ct = default);
    Task<ErrorOr<PartnerInstitutionAdminResponse>> CreatePartnerInstitutionAsync(CreatePartnerInstitutionRequest request, CancellationToken ct = default);
    Task<ErrorOr<PartnerInstitutionAdminResponse>> UpdatePartnerInstitutionAsync(int institutionId, UpdateInstitutionRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeletePartnerInstitutionAsync(int institutionId, CancellationToken ct = default);
    Task<ErrorOr<Updated>> RestorePartnerInstitutionAsync(int institutionId, CancellationToken ct = default);
    Task<ErrorOr<PagedResponse<PartnerCourseResponse>>> GetPartnerCoursesByInstitutionAsync(int institutionId, bool includeDeleted, PagedRequest paging, ExchangeSemester? semester = null, StudyProgramLevel? level = null, string? sortBy = null, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseResponse>> CreatePartnerCourseByInstitutionAsync(int institutionId, CreatePartnerCourseRequest request, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseResponse>> UpdatePartnerCourseAsync(int courseId, UpdatePartnerCourseRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeletePartnerCourseAsync(int courseId, CancellationToken ct = default);
    Task<ErrorOr<Updated>> RestorePartnerCourseAsync(int courseId, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseResponse>> MergePartnerCoursesAsync(MergePartnerCoursesRequest request, CancellationToken ct = default);
    Task<ErrorOr<PartnerCourseUsageResponse>> GetPartnerCourseUsageAsync(int courseId, CancellationToken ct = default);
}
