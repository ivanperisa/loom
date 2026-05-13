namespace Loom.Application.DTOs.Recognition;

public record RecognitionResponse(
    int Id,
    int ExchangeId,
    string Status,
    List<RecognitionEntryResponse> Entries,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
