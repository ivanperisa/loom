namespace Loom.Application.DTOs.Exchange;

public record UpdateLearningAgreementStatusRequest(string Status, string? Message = null);
