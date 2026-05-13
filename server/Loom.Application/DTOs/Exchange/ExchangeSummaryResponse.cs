namespace Loom.Application.DTOs.Exchange;

public record ExchangeSummaryResponse(
    int Id,
    Guid Guid,
    int StudentId,
    string StudentName,
    string? StudentJmbag,
    string PartnerInstitutionName,
    string PartnerProgramName,
    string HomeInstitutionName,
    string HomeProgramName,
    string HomeProfileName,
    string AcademicYear,
    string SemesterType,
    string LearningAgreementStatus,
    string? RecognitionStatus
);
