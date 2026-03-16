namespace ExchangeMapper.Application.DTOs.Exchange;

public class MappingSnapshotResponse
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? CourseCode { get; set; }
    public decimal? AwardedEcts { get; set; }
    public string? ConvertedGrade { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CoordinatorNote { get; set; }
}
