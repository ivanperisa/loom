namespace Loom.Application.DTOs.Exchange;

public record CreateExchangeRequest(
    int HomeProfileId,
    int PartnerInstitutionId,
    string AcademicYear,
    string SemesterType,
    List<int> StudySemesters,
    int? CoordinatorId = null,
    int? TargetStudentId = null,
    string? Mentor = null
);
