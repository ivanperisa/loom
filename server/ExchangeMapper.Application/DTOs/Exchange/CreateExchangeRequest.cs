namespace ExchangeMapper.Application.DTOs.Exchange;

public record CreateExchangeRequest(
    Guid StudyProfileId,
    Guid ForeignProgramId,
    string AcademicYear,
    string SemesterType,
    int StudySemester
);
