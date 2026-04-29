using Loom.Application.DTOs.CourseSlot;

namespace Loom.Application.DTOs.Exchange;

public record ExchangeSnapshotResponse(
    Guid Id,
    Guid ExchangeId,
    string Phase,
    Guid ChangedById,
    string ChangedByName,
    DateTime CreatedAt,
    LearningAgreementSnapshotData? Data
);

public record LearningAgreementSnapshotData(List<SlotStateResponse> SlotStates);
