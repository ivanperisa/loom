using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class LearningAgreementEntry : EntityBase
{
    public Guid LearningAgreementId { get; set; }
    public LearningAgreement LearningAgreement { get; set; } = null!;
    public Guid CourseSlotId { get; set; }
    public CourseSlot CourseSlot { get; set; } = null!;
    public SlotMode Mode { get; set; }
    public Guid? ForeignCourseId { get; set; }
    public ForeignCourse? ForeignCourse { get; set; }
    public decimal? AwardedEcts { get; set; }
    public RecognitionEntry? RecognitionEntry { get; set; }
}
