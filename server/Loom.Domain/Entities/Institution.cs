using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

public class Institution : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? NameHr { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? ErasmusCode { get; set; }
    public InstitutionType Type { get; set; } = InstitutionType.Partner;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<HomeProgram> HomePrograms { get; set; } = [];
    public ICollection<PartnerCourse> PartnerCourses { get; set; } = [];
}
