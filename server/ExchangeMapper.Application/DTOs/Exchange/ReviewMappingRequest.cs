namespace ExchangeMapper.Application.DTOs.Exchange;

public class ReviewMappingRequest
{
    public string Status { get; set; } = string.Empty;
    public string? CoordinatorNote { get; set; }
    public decimal? AwardedEcts { get; set; }
    public string? ConvertedGrade { get; set; }
}
