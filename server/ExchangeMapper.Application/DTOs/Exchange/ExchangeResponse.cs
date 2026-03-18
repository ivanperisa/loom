using ExchangeMapper.Application.DTOs.Institution;

namespace ExchangeMapper.Application.DTOs.Exchange;

public record ExchangeResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    StudyProfileResponse StudyProfile,
    ForeignProgramResponse ForeignProgram,
    Guid? CoordinatorId,
    string? CoordinatorName,
    string? Mentor,
    string AcademicYear,
    string SemesterType,
    int StudySemester,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
