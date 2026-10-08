namespace Loom.Application.Features.Planning;

public record LearningAgreementResponse(
    int ExchangeId,
    string Status,
    string? Message,
    List<HomeSlotResponse> Slots,
    List<LearningAgreementEntryResponse> Entries,
    DateTime? LastModifiedAt,
    string? LastModifiedByName,
    DateTime? SignedAt,
    string? SignedByName,
    int SignedCount
);
