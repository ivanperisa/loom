using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class SlotMapping : EntityBase
{
    public Guid SlotStateId { get; set; }
    public SlotState SlotState { get; set; } = null!;
    public Guid ForeignCourseId { get; set; }
    public ForeignCourse ForeignCourse { get; set; } = null!;
    public decimal AwardedEcts { get; set; }
    public RecognitionEntry? RecognitionEntry { get; set; }
}
