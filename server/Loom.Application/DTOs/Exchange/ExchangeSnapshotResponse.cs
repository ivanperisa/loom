namespace Loom.Application.DTOs.Exchange;

public record ExchangeSnapshotResponse(
    Guid Id,
    string Phase,
    string ChangedByName,
    DateTime CreatedAt
);
