namespace Loom.Application.DTOs.LearningAgreement;

public record MappingExportDto(
    int Version,
    DateTime ExportedAt,
    string ExportedByName,
    MappingExportInstitution Institution,
    MappingExportHomeContext Home,
    List<MappingExportEntry> Mappings
);

public record MappingExportInstitution(int Id, string Name, string? ErasmusCode);

public record MappingExportHomeContext(
    int ProfileId,
    string ProfileName,
    string ProgramName,
    string InstitutionName
);

public record MappingExportEntry(
    int HomeSlotId,
    string HomeSlotLabel,
    int HomeSlotSemester,
    int HomeSlotEcts,
    string Mode,
    MappingExportCourse? PartnerCourse,
    decimal? AwardedEcts
);

public record MappingExportCourse(int Id, string Code, string Name, decimal Ects);
