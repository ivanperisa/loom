namespace Loom.Application.Features.Completion;

public record MappingSchemeResponse(
    int ExchangeId,
    List<MappingSchemeEntryResponse> Entries
);
