using Loom.Application.DTOs.Institution;

namespace Loom.Application.DTOs.Exchange;

public record ExchangeResponse(
    int Id,
    Guid Guid,
    int StudentId,
    string StudentName,
    string? StudentJmbag,
    string HomeInstitutionName,
    string HomeProgramName,
    HomeProfileResponse HomeProfile,
    PartnerProgramResponse PartnerProgram,
    int? CoordinatorId,
    string? CoordinatorName,
    string? Mentor,
    string AcademicYear,
    string SemesterType,
    List<int> StudySemesters,
    string? CoordinatorMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
