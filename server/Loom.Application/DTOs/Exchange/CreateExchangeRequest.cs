namespace Loom.Application.DTOs.Exchange;

public record CreateExchangeRequest(
    int HomeProfileId,
    int PartnerProgramId,
    string AcademicYear,
    string SemesterType,
    int StudySemester
);
