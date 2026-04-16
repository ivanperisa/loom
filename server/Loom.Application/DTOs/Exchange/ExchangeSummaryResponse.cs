namespace Loom.Application.DTOs.Exchange;

public record ExchangeSummaryResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string? StudentJmbag,
    string ForeignInstitutionName,
    string ForeignProgramName,
    string HomeInstitutionName,
    string StudyProgramName,
    string StudyProfileName,
    string AcademicYear,
    string SemesterType,
    string Status
);
