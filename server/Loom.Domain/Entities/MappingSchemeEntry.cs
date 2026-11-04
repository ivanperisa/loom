using Loom.Domain.Common;

namespace Loom.Domain.Entities;

/// <summary>Where a course's result is finally recognised: a home slot and the ECTS it gets there.</summary>
public class MappingSchemeEntry : AuditableEntity
{
    public int RecognitionEntryId { get; set; }
    public RecognitionEntry RecognitionEntry { get; set; } = null!;

    public int HomeSlotId { get; set; }
    public HomeSlot HomeSlot { get; set; } = null!;

    public decimal AwardedEcts { get; set; }
}
