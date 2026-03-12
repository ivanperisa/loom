namespace ExchangeMapper.Application.DTOs.Responses;

public class CourseDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public int Ects { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? LecturesHours { get; set; }
    public int? AuditoryHours { get; set; }
    public int? LabHours { get; set; }
}
