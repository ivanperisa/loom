namespace Loom.Application.DTOs.LearningAgreement;

public record LearningAgreementResponse(
    int ExchangeId,
    string Status,
    List<HomeSlotResponse> Slots,
    List<LearningAgreementEntryResponse> Entries
);
