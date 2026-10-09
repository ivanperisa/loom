using Loom.Domain.Enums;

namespace Loom.Application.Features.Planning;

public record LearningAgreementResponse(
    int ExchangeId,
    DocumentStatus Status,
    string? Message,
    List<HomeSlotResponse> Slots,
    List<LearningAgreementEntryResponse> Entries,
    DateTime? LastModifiedAt,
    string? LastModifiedByName,
    DateTime? SignedAt,
    string? SignedByName,
    /// <summary>Number of approved versions (1 = original only, 2 = up to A1, …).</summary>
    int SignedCount,
    /// <summary>"Start final recognition" was pressed: the LA and table 1 can never change again.</summary>
    bool IsConcluded,
    DateTime? ConcludedAt,
    string? ConcludedByName);
