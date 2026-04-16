using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class StudyProfile : EntityBase
{
    public Guid StudyProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    public StudyProgram StudyProgram { get; set; } = null!;
    public ICollection<CourseSlot> CourseSlots { get; set; } = null!;
}
