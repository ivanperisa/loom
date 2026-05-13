using Loom.Application.DTOs.LearningAgreement;

namespace Loom.Application.DTOs.Exchange;

public record ExchangeSnapshotResponse(
    int Id,
    int ExchangeId,
    string Phase,
    int ChangedById,
    string ChangedByName,
    DateTime CreatedAt,
    LearningAgreementSnapshotData? Data
);

public record LearningAgreementSnapshotData(List<LearningAgreementEntryResponse> Entries);
