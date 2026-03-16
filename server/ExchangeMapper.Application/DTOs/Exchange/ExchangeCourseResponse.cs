namespace ExchangeMapper.Application.DTOs.Exchange;

public class ExchangeCourseResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? NameHr { get; set; }
    public decimal? Ects { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? LecturesHours { get; set; }
    public int? AuditoryHours { get; set; }
    public int? LabHours { get; set; }
    public string? OriginalGrade { get; set; }
    public string? EctsGrade { get; set; }
    public DateOnly? ExamDate { get; set; }
    public List<CourseMappingResponse> Mappings { get; set; } = [];
}
