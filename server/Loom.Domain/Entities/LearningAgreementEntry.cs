using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class LearningAgreementEntry : EntityBase
{
    public int LearningAgreementId { get; set; }
    public LearningAgreement LearningAgreement { get; set; } = null!;
    public int HomeSlotId { get; set; }
    public HomeSlot HomeSlot { get; set; } = null!;
    public SlotMode Mode { get; set; }
    public int? PartnerCourseId { get; set; }
    public PartnerCourse? PartnerCourse { get; set; }
    public decimal? AwardedEcts { get; set; }

    /// <summary>
    /// Taken out after it was approved: marked for removal in the current draft, or removed in <see cref="RemovedInVersion"/>.
    /// The row stays so the LA keeps its history (shown struck through with "removed in An").
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>The approved version this component first appeared in (1 = original, n = amendment A(n-1)). Null until approved.</summary>
    public int? AddedInVersion { get; set; }

    /// <summary>The approved version it was gone from. Null while it is live or while its removal awaits approval.</summary>
    public int? RemovedInVersion { get; set; }
}
