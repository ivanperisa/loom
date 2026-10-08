namespace Loom.Application.Features.Planning;

public record MappingImportResult(
    int AppliedCount,
    List<MappingImportSkip> Skipped
);

public record MappingImportSkip(int HomeSlotId, string HomeSlotLabel, string Reason);
