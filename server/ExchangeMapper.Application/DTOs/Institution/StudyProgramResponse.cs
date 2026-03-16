namespace ExchangeMapper.Application.DTOs.Institution;

public class StudyProgramResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int DurationSemesters { get; set; }
    public string IscedCode { get; set; } = string.Empty;
}
