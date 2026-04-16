using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class CourseSlot : EntityBase
{
    public Guid StudyProfileId { get; set; }
    public StudyProfile StudyProfile { get; set; } = null!;
    public int Semester { get; set; }
    public int ColStart { get; set; }
    public int Ects { get; set; }
    public CourseSlotCategory Category { get; set; }
    public string? CourseCode { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameEn { get; set; }
    public string Color { get; set; } = null!;
    public ICollection<SlotState> SlotStates { get; set; } = null!;
}
