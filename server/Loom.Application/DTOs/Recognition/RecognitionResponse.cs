namespace Loom.Application.DTOs.Recognition;

public record RecognitionResponse(
    Guid Id,
    Guid ExchangeId,
    string Status,
    List<RecognitionEntryResponse> Entries,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
