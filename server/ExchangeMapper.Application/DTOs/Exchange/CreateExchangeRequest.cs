namespace ExchangeMapper.Application.DTOs.Exchange;

public class CreateExchangeRequest
{
    public Guid ForeignInstitutionId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public int? DurationMonths { get; set; }
    public string? Mentor { get; set; }
}
