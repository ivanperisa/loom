using Loom.Application.Common.Querying;
using Loom.Domain.Enums;

namespace Loom.Application.Features.Catalog;

// ---- home institution (FER) catalogue
public record InstitutionResponse(int Id, string Name, string? NameHr, string? Country, string? City, string? ErasmusCode);
public record HomeProgramResponse(int Id, string Name, string? NameEn, string Level, int DurationSemesters, List<HomeProfileResponse> Profiles);
public record HomeProfileResponse(int Id, string Name, string? NameEn);

// ---- partner institutions
public record PartnerInstitutionListQuery : ListQuery
{
    public string? Country { get; init; }
    public bool IncludeDeleted { get; init; }
}

public record PartnerInstitutionRequest(string Name, string? NameHr, string Country, string? City = null, string? ErasmusCode = null);

public record PartnerInstitutionAdminResponse(
    int Id, string Name, string? NameHr, string Country, string? City, string? ErasmusCode, int CourseCount, bool IsDeleted);

// ---- partner courses
public record PartnerCourseListQuery : ListQuery
{
    public ExchangeSemester? Semester { get; init; }
    public StudyProgramLevel? Level { get; init; }
    public bool IncludeDeleted { get; init; }
}

public record PartnerCourseRequest(
    string Code,
    string Name,
    string? NameHr,
    decimal Ects,
    string Semester,
    string Level,
    int? LecturesH = null,
    int? AuditoryH = null,
    int? LabH = null,
    string? Url = null);

public record PartnerCourseResponse(
    int Id,
    string Code,
    string Name,
    string? NameHr,
    string? Url,
    decimal Ects,
    int? LecturesH,
    int? AuditoryH,
    int? LabH,
    string Semester,
    string Level,
    bool IsDeleted);

public record MergePartnerCoursesRequest(int PrimaryCourseId, List<int> DuplicateCourseIds);

public record PartnerCourseUsageResponse(int ExchangeCount, List<PartnerCourseUsageGroup> Groups);

public record PartnerCourseUsageGroup(
    string ProgramName,
    string ProfileName,
    int? RecognizedAsIsvuCode,
    string RecognizedAsName,
    bool IsCourseGroup,
    int ExchangeCount,
    decimal TotalAwardedEcts,
    List<string> AcademicYears);
