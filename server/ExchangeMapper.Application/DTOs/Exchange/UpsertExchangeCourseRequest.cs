namespace ExchangeMapper.Application.DTOs.Exchange;

public class UpsertExchangeCourseRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal? Ects { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? LecturesHours { get; set; }
    public int? AuditoryHours { get; set; }
    public int? LabHours { get; set; }
}
