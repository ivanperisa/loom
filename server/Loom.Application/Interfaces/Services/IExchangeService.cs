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
    Task<ErrorOr<LearningAgreementResponse>> SetSlotModeAsync(Guid exchangeId, Guid requesterId, SetSlotModeRequest request, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> AddSlotMappingAsync(Guid exchangeId, Guid requesterId, AddSlotMappingRequest request, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> RemoveSlotMappingAsync(Guid exchangeId, Guid requesterId, RemoveSlotMappingRequest request, CancellationToken ct = default);
    Task<ErrorOr<LearningAgreementResponse>> RemoveSlotStateAsync(Guid exchangeId, Guid requesterId, Guid courseSlotId, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(Guid coordinatorId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeId, Guid requesterId, string? message, CancellationToken ct = default);
}
