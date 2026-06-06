namespace Loom.Application.DTOs.Exchange;

public record CreateExchangeRequest(
    int HomeProfileId,
    int PartnerProgramId,
    string AcademicYear,
    string SemesterType,
    List<int> StudySemesters,
    int? CoordinatorId = null,
    int? TargetStudentId = null
);
