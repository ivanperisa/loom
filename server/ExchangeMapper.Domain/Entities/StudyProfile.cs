using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class StudyProfile : EntityBase
{
    public Guid StudyProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int? ExchangeSemesters { get; set; }
    public string? ExchangeSemesterType { get; set; }
    public int? ExchangeSpots { get; set; }

    public StudyProgram StudyProgram { get; set; } = null!;
    public ICollection<UserInstitution> UserInstitutions { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
}
