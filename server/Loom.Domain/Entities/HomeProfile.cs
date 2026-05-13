using Loom.Domain.Common;

namespace Loom.Domain.Entities;

public class HomeProfile : EntityBase
{
    public int ProgramId { get; set; }
    public HomeProgram Program { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    public ICollection<HomeSlot> Slots { get; set; } = null!;
}
