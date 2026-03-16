using ExchangeMapper.Application.DTOs.Institution;

namespace ExchangeMapper.Application.DTOs.Exchange;

public class ExchangeResponse
{
    public Guid Id { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public int? DurationMonths { get; set; }
    public string? Mentor { get; set; }
    public string Status { get; set; } = string.Empty;
    public InstitutionResponse ForeignInstitution { get; set; } = null!;
    public List<ExchangeCourseResponse> Courses { get; set; } = [];
}
