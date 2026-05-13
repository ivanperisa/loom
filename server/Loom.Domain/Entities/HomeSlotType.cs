using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class HomeSlotType : EntityBase
{
    public string Name { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string Color { get; set; } = null!;

    public ICollection<HomeSlot> Slots { get; set; } = [];
    public ICollection<HomeCourseGroup> CourseGroups { get; set; } = [];
}
