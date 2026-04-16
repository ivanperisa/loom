using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class SlotState : EntityBase
{
    public Guid ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public Guid CourseSlotId { get; set; }
    public CourseSlot CourseSlot { get; set; } = null!;
    public SlotMode Mode { get; set; }
    public ICollection<SlotMapping> SlotMappings { get; set; } = null!;
}
