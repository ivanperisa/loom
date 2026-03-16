namespace ExchangeMapper.Application.DTOs.Institution;

public class StudyProfileResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int? ExchangeSemesters { get; set; }
    public string? ExchangeSemesterType { get; set; }
    public int? ExchangeSpots { get; set; }
}
