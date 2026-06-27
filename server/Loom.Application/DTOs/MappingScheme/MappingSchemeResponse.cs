namespace Loom.Application.DTOs.MappingScheme;

public record MappingSchemeResponse(
    int ExchangeId,
    List<MappingSchemeEntryResponse> Entries
);
