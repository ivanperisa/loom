using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class Institution : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? ErasmusCode { get; set; }
    public bool IsHome { get; set; }

    public ICollection<StudyProgram> StudyPrograms { get; set; } = [];
    public ICollection<ForeignProgram> ForeignPrograms { get; set; } = null!;
}
