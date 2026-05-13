using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class Institution : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? ErasmusCode { get; set; }
    public InstitutionType Type { get; set; } = InstitutionType.Partner;

    public ICollection<HomeProgram> HomePrograms { get; set; } = [];
    public ICollection<PartnerProgram> PartnerPrograms { get; set; } = null!;
}
