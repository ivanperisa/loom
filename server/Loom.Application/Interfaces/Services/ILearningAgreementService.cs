using ErrorOr;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;

namespace Loom.Application.Interfaces.Services;

public interface ILearningAgreementService
{
    Task<ErrorOr<LearningAgreementResponse>> GetLearningAgreementAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> SaveLearningAgreementAsync(Guid exchangeGuid, int requesterId, SaveLearningAgreementRequest request, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateLearningAgreementStatusAsync(Guid exchangeGuid, int requesterId, UpdateLearningAgreementStatusRequest request, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> UpdateLearningAgreementMessageAsync(Guid exchangeGuid, int requesterId, string? message, CancellationToken ct = default);
    Task<ErrorOr<MappingExportDto>> ExportMappingsAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<MappingImportResult>> ImportMappingsAsync(Guid exchangeGuid, int requesterId, MappingExportDto dto, CancellationToken ct = default);
    Task<ErrorOr<List<LaSnapshotSummary>>> GetLearningAgreementHistoryAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<List<SnapshotListItem>>> GetSnapshotsAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<Updated>> RestoreSnapshotAsync(Guid exchangeGuid, int snapshotId, int requesterId, CancellationToken ct = default);
}
