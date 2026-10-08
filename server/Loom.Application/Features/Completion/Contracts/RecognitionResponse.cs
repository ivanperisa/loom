namespace Loom.Application.Features.Completion;

public record RecognitionResponse(
    int Id,
    int ExchangeId,
    string Status,
    string? Message,
    List<RecognitionEntryResponse> Entries,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastModifiedAt,
    string? LastModifiedByName,
    DateTime? SignedAt,
    string? SignedByName
);
