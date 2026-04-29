namespace Loom.Domain.Entities;

public class CourseCategory
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string Color { get; set; } = null!;

    public ICollection<CourseSlot> CourseSlots { get; set; } = [];
}
