namespace Loom.Application.DTOs.Recognition;

public record SaveRecognitionRequest(List<UpsertRecognitionEntryRequest> Entries);