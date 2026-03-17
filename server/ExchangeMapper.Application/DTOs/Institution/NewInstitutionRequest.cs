using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.DTOs.Institution;

public class NewInstitutionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? ErasmusCode { get; set; }
    public string? IscedCode { get; set; }
    public string? ProgramName { get; set; }
    public string? ProgramNameEn { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfileNameEn { get; set; }
    public StudyProgramLevel Level { get; set; } = StudyProgramLevel.Undergraduate;
    public int DurationSemesters { get; set; } = 0;
}
