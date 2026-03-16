namespace ExchangeMapper.Application.DTOs.Institution;

public class NewStudyProfileRequest
{
    public Guid StudyProgramId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string? ProfileNameEn { get; set; }
}
