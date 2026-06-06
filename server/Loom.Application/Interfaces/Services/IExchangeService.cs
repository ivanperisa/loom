using ErrorOr;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;

namespace Loom.Application.Interfaces.Services;

public interface IExchangeService
{
    Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(int requesterId, CreateExchangeRequest request, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> GetExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyExchangesAsync(int studentId, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateLearningAgreementStatusAsync(Guid exchangeGuid, int requesterId, UpdateLearningAgreementStatusRequest request, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> GetLearningAgreementAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> SaveLearningAgreementAsync(Guid exchangeGuid, int requesterId, SaveLearningAgreementRequest request, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(int coordinatorId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeGuid, int requesterId, string? message, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSnapshotResponse>>> GetSnapshotsAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeSnapshotResponse>> GetSnapshotAsync(Guid exchangeGuid, int snapshotId, int requesterId, CancellationToken ct = default);
}
