using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class ForeignProgram : EntityBase
{
    public Guid InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? NameEn { get; set; }
    public ICollection<ForeignCourse> Courses { get; set; } = null!;
}
