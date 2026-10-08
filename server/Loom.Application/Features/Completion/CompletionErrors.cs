using ErrorOr;

namespace Loom.Application.Features.Completion;

public static class CompletionErrors
{
    public static Error NotStarted => Error.Conflict("RECOGNITION_NOT_STARTED", "Final recognition has not started yet.");
    public static Error AlreadyStarted => Error.Conflict("RECOGNITION_ALREADY_STARTED", "Final recognition has already started.");
    public static Error LaNotApproved => Error.Conflict("LA_NOT_APPROVED", "The learning agreement must be approved first.");
    public static Error Locked => Error.Conflict("RECOGNITION_LOCKED", "The recognition is approved. The coordinator has to send it back to draft before it can change.");
    public static Error InvalidStatus => Error.Validation("INVALID_STATUS", "Status must be Draft or Approved.");
    public static Error StatusUnchanged(object status) => Error.Conflict("STATUS_UNCHANGED", $"Recognition is already {status}.");
    public static Error NotAssignedCoordinator => Error.Forbidden("FORBIDDEN", "Only the assigned coordinator can approve or reopen the recognition.");
    public static Error InvalidEnrollmentStatus(string? value) => Error.Validation("INVALID_ENROLLMENT_STATUS", $"Invalid course status: {value}.");
    public static Error GradeTooLong(string field, int max) => Error.Validation("INVALID_GRADE", $"{field} can be at most {max} characters.");
    public static Error CourseNotInScheme(int courseId) => Error.Validation("INVALID_PARTNER_COURSE", $"Course {courseId} is not part of the mapping scheme.");
    public static Error EntryNotFound(int id) => Error.NotFound("ENTRY_NOT_FOUND", $"Mapping scheme entry {id} not found.");
    public static Error NegativeEcts => Error.Validation("INVALID_ECTS", "Awarded ECTS cannot be negative.");
    public static Error EctsExceeded(int courseId, decimal available) => Error.Validation("ECTS_EXCEEDED", $"Awarded ECTS for course {courseId} exceeds available {available}.");
    public static Error SlotNotInProfile(int slotId) => Error.Validation("SLOT_NOT_IN_PROFILE", $"Home slot {slotId} does not belong to this profile.");
}
