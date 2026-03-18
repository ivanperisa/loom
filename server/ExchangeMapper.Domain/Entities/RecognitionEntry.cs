using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class RecognitionEntry : EntityBase
{
    public Guid RecognitionId { get; set; }
    public Recognition Recognition { get; set; } = null!;
    public Guid SlotMappingId { get; set; }
    public SlotMapping SlotMapping { get; set; } = null!;
    public string? EnrollmentStatus { get; set; }
    public string? OriginalGrade { get; set; }
    public string? EctsGrade { get; set; }
    public string? HrGrade { get; set; }
    public DateOnly? ExamDate { get; set; }
}
