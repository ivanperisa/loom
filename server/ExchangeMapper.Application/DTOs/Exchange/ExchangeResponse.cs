using ExchangeMapper.Application.DTOs.Institution;

namespace ExchangeMapper.Application.DTOs.Exchange;

public record ExchangeResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string HomeInstitutionName,
    string StudyProgramName,
    StudyProfileResponse StudyProfile,
    ForeignProgramResponse ForeignProgram,
    Guid? CoordinatorId,
    string? CoordinatorName,
    string? Mentor,
    string AcademicYear,
    string SemesterType,
    int StudySemester,
    string Status,
    string? CoordinatorMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
