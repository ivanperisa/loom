namespace ExchangeMapper.Application.DTOs.Exchange;

public class CoordinatorStudentSummaryResponse
{
    public Guid ExchangeId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentJmbag { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ForeignInstitutionName { get; set; } = string.Empty;
    public string? ForeignInstitutionCountry { get; set; }
    public int TotalCourses { get; set; }
    public int PendingMappings { get; set; }
    public int ApprovedMappings { get; set; }
}
