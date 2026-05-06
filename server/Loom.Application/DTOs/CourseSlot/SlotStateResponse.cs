namespace Loom.Application.DTOs.CourseSlot;

// Kept for backward compatibility with snapshot deserialization only.
// Active code uses LearningAgreementEntryResponse directly.
public record LearningAgreementLineResponse(
    Guid Id,
    Guid CourseSlotId,
    string Mode,
    List<LearningAgreementEntryResponse> Entries
);
