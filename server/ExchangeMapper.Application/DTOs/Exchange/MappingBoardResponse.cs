namespace ExchangeMapper.Application.DTOs.Exchange;

public class MappingBoardResponse
{
    public List<FerCourseGroupResponse> FerCourseGroups { get; set; } = [];
    public List<ExchangeCourseWithMappingsResponse> ExchangeCourses { get; set; } = [];
}

public class FerCourseGroupResponse
{
    public string Type { get; set; } = string.Empty;
    public List<FerCourseResponse> Courses { get; set; } = [];
}

public class FerCourseResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int Ects { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class ExchangeCourseWithMappingsResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal? Ects { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<MappingRowResponse> Mappings { get; set; } = [];
}

public class MappingRowResponse
{
    public Guid Id { get; set; }
    public Guid FerCourseId { get; set; }
    public string FerCourseName { get; set; } = string.Empty;
    public string? FerCourseCode { get; set; }
    public decimal? AwardedEcts { get; set; }
    public string? ConvertedGrade { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CoordinatorNote { get; set; }
}
