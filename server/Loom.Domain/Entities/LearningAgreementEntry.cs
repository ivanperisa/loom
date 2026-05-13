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
    public bool IsDeleted { get; set; }

    public RecognitionEntry? RecognitionEntry { get; set; }
}
