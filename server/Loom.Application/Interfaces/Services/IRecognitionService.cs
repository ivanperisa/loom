using ErrorOr;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.Recognition;

namespace Loom.Application.Interfaces.Services;

public interface IRecognitionService
{
    Task<ErrorOr<RecognitionResponse>> GetOrCreateRecognitionAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> SaveRecognitionAsync(Guid exchangeId, Guid studentId, SaveRecognitionRequest request, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> UpdateRecognitionStatusAsync(Guid exchangeId, Guid requesterId, UpdateExchangeStatusRequest request, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> SetEntryRecognizedAsync(Guid exchangeId, Guid entryId, Guid coordinatorId, SetEntryRecognizedRequest request, CancellationToken ct = default);
}
