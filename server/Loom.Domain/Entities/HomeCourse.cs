using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class HomeCourse : EntityBase
{
    public int IsvuCode { get; set; }
    public string Name { get; set; } = null!;
    public string? NameEn { get; set; }

    public ICollection<HomeSlot> Slots { get; set; } = [];
}
