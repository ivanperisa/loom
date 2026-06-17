namespace Loom.Application.DTOs.LearningAgreement;

public record LearningAgreementResponse(
    int ExchangeId,
    string Status,
    string? Message,
    List<HomeSlotResponse> Slots,
    List<LearningAgreementEntryResponse> Entries,
    DateTime? LastModifiedAt,
    string? LastModifiedByName,
    DateTime? SignedAt,
    string? SignedByName
);
