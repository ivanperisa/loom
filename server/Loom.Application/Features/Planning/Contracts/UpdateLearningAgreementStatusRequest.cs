namespace Loom.Application.Features.Planning;

/// <summary>Status is "Approved" or "Draft". The message (optional) is shown to the student when sending back to draft.</summary>
public record UpdateLearningAgreementStatusRequest(string Status, string? Message = null);
