using ErrorOr;
using Loom.Application.DTOs.CourseSlot;
using Loom.Application.DTOs.Exchange;

namespace Loom.Application.Interfaces.Services;

public interface IExchangeService
{
    Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(Guid studentId, CreateExchangeRequest request, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> GetExchangeAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyExchangesAsync(Guid studentId, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateExchangeStatusAsync(Guid exchangeId, Guid requesterId, UpdateExchangeStatusRequest request, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> GetLearningAgreementAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> SaveLearningAgreementAsync(Guid exchangeId, Guid requesterId, SaveLearningAgreementRequest request, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(Guid coordinatorId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeId, Guid requesterId, string? message, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSnapshotResponse>>> GetSnapshotsAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeSnapshotResponse>> GetSnapshotAsync(Guid exchangeId, Guid snapshotId, Guid requesterId, CancellationToken ct = default);
}
