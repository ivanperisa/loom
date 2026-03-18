namespace ExchangeMapper.Application.DTOs.Exchange;

public record ExchangeSummaryResponse(
    Guid Id,
    string StudentName,
    string? StudentJmbag,
    string ForeignInstitutionName,
    string ForeignProgramName,
    string StudyProfileName,
    string AcademicYear,
    string SemesterType,
    string Status
);
