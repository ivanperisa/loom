using ErrorOr;
using Loom.Application.DTOs.Recognition;

namespace Loom.Application.Interfaces.Services;

public interface IRecognitionService
{
    Task<ErrorOr<RecognitionResponse>> GetOrCreateRecognitionAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> SaveRecognitionAsync(Guid exchangeGuid, int studentId, SaveRecognitionRequest request, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> UpdateRecognitionStatusAsync(Guid exchangeGuid, int requesterId, UpdateRecognitionStatusRequest request, CancellationToken ct = default);
    Task<ErrorOr<RecognitionResponse>> SetEntryRecognizedAsync(Guid exchangeGuid, int entryId, int coordinatorId, SetEntryRecognizedRequest request, CancellationToken ct = default);
}
