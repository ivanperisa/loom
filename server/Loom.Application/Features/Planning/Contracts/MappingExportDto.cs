namespace Loom.Application.Features.Planning;

/// <summary>
/// "Export for import": the LA's mappings with enough context to import them into another exchange.
/// Version 2 (current) and version 1 (older exports) have the same shape.
/// </summary>
public record MappingExportDto(
    int Version,
    DateTime ExportedAt,
    string ExportedByName,
    MappingExportInstitution Institution,
    MappingExportHomeContext Home,
    List<MappingExportEntry> Mappings,
    string? Format = MappingExportDto.FormatName)
{
    public const string FormatName = "loom.learning-agreement";
    public const int CurrentVersion = 2;
}

public record MappingExportInstitution(int Id, string Name, string? ErasmusCode);

public record MappingExportHomeContext(
    int ProfileId,
    string ProfileName,
    string ProgramName,
    string InstitutionName);

public record MappingExportEntry(
    int HomeSlotId,
    string HomeSlotLabel,
    int HomeSlotSemester,
    int HomeSlotEcts,
    string Mode,
    MappingExportCourse? PartnerCourse,
    decimal? AwardedEcts);

public record MappingExportCourse(int Id, string Code, string Name, decimal Ects);
