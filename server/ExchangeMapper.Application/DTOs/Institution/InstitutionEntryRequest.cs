namespace ExchangeMapper.Application.DTOs.Institution;

public class InstitutionEntryRequest
{
    public Guid? ExistingStudyProfileId { get; set; }
    public Guid? ExistingInstitutionId { get; set; }
    public NewStudyProfileRequest? NewStudyProfile { get; set; }
    public NewInstitutionRequest? NewInstitution { get; set; }
}
