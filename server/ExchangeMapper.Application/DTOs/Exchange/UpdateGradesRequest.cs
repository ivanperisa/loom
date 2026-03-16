namespace ExchangeMapper.Application.DTOs.Exchange;

public class UpdateGradesRequest
{
    public string? OriginalGrade { get; set; }
    public string? EctsGrade { get; set; }
    public DateOnly? ExamDate { get; set; }
}
