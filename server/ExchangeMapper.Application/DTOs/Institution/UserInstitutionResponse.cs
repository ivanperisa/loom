namespace ExchangeMapper.Application.DTOs.Institution;

public class UserInstitutionResponse
{
    public Guid UserInstitutionId { get; set; }
    public bool HasActiveExchanges { get; set; }
    public InstitutionResponse Institution { get; set; } = null!;
    public StudyProgramResponse? StudyProgram { get; set; }
    public StudyProfileResponse? StudyProfile { get; set; }
}
