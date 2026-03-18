using ErrorOr;
using ExchangeMapper.Application.DTOs.Exchange;
using ExchangeMapper.Application.DTOs.Recognition;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IRecognitionService
{
    Task<ErrorOr<RecognitionResponse>> GetOrCreateRecognitionAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> UpsertEntryAsync(Guid exchangeId, Guid studentId, UpsertRecognitionEntryRequest request, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> UpdateRecognitionStatusAsync(Guid exchangeId, Guid requesterId, UpdateExchangeStatusRequest request, CancellationToken ct = default);
}
