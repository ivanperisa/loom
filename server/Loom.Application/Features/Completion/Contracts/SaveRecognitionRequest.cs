namespace Loom.Application.Features.Completion;

public record SaveRecognitionRequest(List<UpsertRecognitionEntryRequest> Entries);