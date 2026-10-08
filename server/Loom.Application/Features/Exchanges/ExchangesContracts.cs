using Loom.Application.Features.Catalog;

namespace Loom.Application.Features.Exchanges;

public record ExchangeResponse(
    int Id,
    Guid Guid,
    int StudentId,
    string StudentName,
    string? StudentJmbag,
    string HomeInstitutionName,
    string HomeProgramName,
    HomeProfileResponse HomeProfile,
    int PartnerInstitutionId,
    string PartnerInstitutionName,
    int? CoordinatorId,
    string? CoordinatorName,
    string? Mentor,
    string AcademicYear,
    string SemesterType,
    List<int> StudySemesters,
    string? CoordinatorMessage,
    string? EwpLink,
    bool StudentIsPlaceholder,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ExchangeSummaryResponse(
    int Id,
    Guid Guid,
    int StudentId,
    string StudentName,
    string? StudentJmbag,
    string PartnerInstitutionName,
    string HomeInstitutionName,
    string HomeProgramName,
    string HomeProfileName,
    string AcademicYear,
    string SemesterType,
    string LearningAgreementStatus,
    string? RecognitionStatus,
    string? EwpLink);

public record CreateExchangeRequest(
    int HomeProfileId,
    int PartnerInstitutionId,
    string AcademicYear,
    string SemesterType,
    List<int> StudySemesters,
    int? CoordinatorId = null,
    int? TargetStudentId = null,
    string? Mentor = null);

public record UpdateExchangeRequest(
    string AcademicYear,
    string SemesterType,
    List<int> StudySemesters,
    int? CoordinatorId = null,
    string? Mentor = null,
    string? EwpLink = null);

public record UpdateCoordinatorMessageRequest(string? Message);
