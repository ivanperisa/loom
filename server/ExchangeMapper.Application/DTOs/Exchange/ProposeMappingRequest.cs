namespace ExchangeMapper.Application.DTOs.Exchange;

public class ProposeMappingRequest
{
    public List<MappingEntry> Mappings { get; set; } = [];
}

public class MappingEntry
{
    public Guid CourseId { get; set; }
    public decimal? AwardedEcts { get; set; }
}
