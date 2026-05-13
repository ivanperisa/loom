using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class HomeSlot : EntityBase
{
    public int ProfileId { get; set; }
    public HomeProfile Profile { get; set; } = null!;
    public int Semester { get; set; }
    public int SlotPosition { get; set; }
    public int Ects { get; set; }

    public int SlotTypeId { get; set; }
    public HomeSlotType SlotType { get; set; } = null!;

    public int? CourseId { get; set; }
    public HomeCourse? Course { get; set; }

    public int? CourseGroupId { get; set; }
    public HomeCourseGroup? CourseGroup { get; set; }
}
