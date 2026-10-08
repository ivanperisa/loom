using ErrorOr;

namespace Loom.Application.Features.Planning;

public static class PlanningErrors
{
    public static Error Locked => Error.Conflict("LA_LOCKED", "The learning agreement is approved. The coordinator has to send it back to draft before it can change.");
    public static Error Concluded => Error.Conflict("LA_CONCLUDED", "Final recognition has started: the learning agreement can no longer change.");
    public static Error NotFound => Error.NotFound("LA_NOT_FOUND", "Learning agreement not found.");
    public static Error InvalidStatus => Error.Validation("INVALID_STATUS", "Status must be Draft or Approved.");
    public static Error NotAssignedCoordinator => Error.Forbidden("FORBIDDEN", "Only the assigned coordinator can approve or reopen the learning agreement.");
    public static Error StatusUnchanged(object status) => Error.Conflict("STATUS_UNCHANGED", $"Learning agreement is already {status}.");
    public static Error InvalidMode(string mode) => Error.Validation("INVALID_MODE", $"Invalid slot mode: {mode}.");
    public static Error CourseOnNonExchangeSlot => Error.Validation("INVALID_MAPPING", "Partner courses are only allowed on slots marked as AtExchange.");
    public static Error MixedSlotModes(int slotId) => Error.Validation("MIXED_SLOT_MODES", $"Home slot {slotId} has entries with different modes.");
    public static Error DuplicateEntry(int slotId) => Error.Validation("DUPLICATE_ENTRY", $"Home slot {slotId} lists the same course twice.");
    public static Error SlotNotInProfile(int slotId) => Error.Validation("SLOT_NOT_IN_PROFILE", $"Home slot {slotId} does not belong to this profile.");
    public static Error PartnerCourseNotFound(int courseId) => Error.NotFound("PARTNER_COURSE_NOT_FOUND", $"Partner course {courseId} not found.");
    public static Error CourseNotAtPartner(int courseId) => Error.Validation("COURSE_NOT_AT_PARTNER", $"Course {courseId} is not offered by this exchange's partner institution.");
    public static Error EctsRequired(int slotId) => Error.Validation("ECTS_REQUIRED", $"Home slot {slotId}: a mapped course needs a positive number of ECTS.");
    public static Error EctsExceeded(int courseId, decimal available) => Error.Validation("ECTS_EXCEEDED", $"Awarded ECTS for course {courseId} exceeds available {available}.");
    public static Error InvalidImportFile => Error.Validation("INVALID_IMPORT_FILE", "This is not a learning agreement export (or its version is not supported).");
    public static Error VersionNotFound => Error.NotFound("VERSION_NOT_FOUND", "Version not found.");
    public static Error VersionUnreadable => Error.Validation("VERSION_UNREADABLE", "This version cannot be restored (its data is in an old or damaged format).");
}
