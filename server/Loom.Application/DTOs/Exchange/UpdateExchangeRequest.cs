namespace Loom.Application.DTOs.Exchange;

public record UpdateExchangeRequest(
    string AcademicYear,
    string SemesterType,
    List<int> StudySemesters,
    int? CoordinatorId = null,
    string? Mentor = null,
    string? EwpLink = null
);
