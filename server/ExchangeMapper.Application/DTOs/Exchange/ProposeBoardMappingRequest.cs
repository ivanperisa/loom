namespace ExchangeMapper.Application.DTOs.Exchange;

public class ProposeBoardMappingRequest
{
    public List<ExchangeCourseMappingRequest> Courses { get; set; } = [];
}

public class ExchangeCourseMappingRequest
{
    public Guid ExchangeCourseId { get; set; }
    public List<MappingEntryRequest> Mappings { get; set; } = [];
}

public class MappingEntryRequest
{
    public Guid FerCourseId { get; set; }
    public decimal? AwardedEcts { get; set; }
    public string? ConvertedGrade { get; set; }
    public string? CoordinatorNote { get; set; }
}
