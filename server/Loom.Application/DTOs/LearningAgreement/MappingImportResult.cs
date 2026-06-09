namespace Loom.Application.DTOs.LearningAgreement;

public record MappingImportResult(
    int AppliedCount,
    List<MappingImportSkip> Skipped
);

public record MappingImportSkip(int HomeSlotId, string HomeSlotLabel, string Reason);
