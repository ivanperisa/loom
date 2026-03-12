namespace ExchangeMapper.Application.DTOs.Responses;

public class StudyProgramResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Level { get; set; } = string.Empty;
    public int DurationSemesters { get; set; }
    public string? IscedCode { get; set; }
}
