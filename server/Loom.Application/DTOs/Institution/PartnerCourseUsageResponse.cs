namespace Loom.Application.DTOs.Institution;

public record PartnerCourseUsageResponse(
    int ExchangeCount,
    List<PartnerCourseUsageGroup> Groups
);

public record PartnerCourseUsageGroup(
    string ProgramName,
    string ProfileName,
    int? RecognizedAsIsvuCode,
    string RecognizedAsName,
    bool IsCourseGroup,
    int ExchangeCount,
    decimal TotalAwardedEcts,
    List<string> AcademicYears
);
