namespace ExchangeMapper.Application.DTOs.Exchange;

public class MappingHistoryResponse
{
    public Guid Id { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ExchangeCourseName { get; set; } = string.Empty;
    public string? ExchangeCourseCode { get; set; }
    public MappingSnapshotResponse Snapshot { get; set; } = null!;
}
