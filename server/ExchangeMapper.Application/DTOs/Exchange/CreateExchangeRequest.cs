namespace ExchangeMapper.Application.DTOs.Exchange;

public record CreateExchangeRequest(
    Guid StudyProfileId,
    Guid ForeignProgramId,
    Guid? CoordinatorId,
    string? Mentor,
    string AcademicYear,
    string SemesterType,
    int StudySemester
);
