using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class CourseSlot : EntityBase
{
    public Guid StudyProfileId { get; set; }
    public StudyProfile StudyProfile { get; set; } = null!;
    public int Semester { get; set; }
    public int SlotPosition { get; set; }
    public int Ects { get; set; }
    public string CategoryCode { get; set; } = null!;
    public CourseCategory Category { get; set; } = null!;
    public string? CourseCode { get; set; }
    public string CourseName { get; set; } = null!;
    public string? CourseNameEn { get; set; }
}
