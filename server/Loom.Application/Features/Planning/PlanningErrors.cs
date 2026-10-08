using ErrorOr;

namespace Loom.Application.Features.Planning;

public static class PlanningErrors
{
    public static Error Locked => Error.Conflict("LA_LOCKED", "Learning agreement cannot be modified in current status.");
    public static Error NotFound => Error.NotFound("LA_NOT_FOUND", "Learning agreement not found.");
    public static Error InvalidStatus => Error.Validation("INVALID_STATUS", "Invalid status.");
    public static Error NotAssignedCoordinator => Error.Forbidden("FORBIDDEN", "Only the assigned coordinator can change the learning agreement status.");
    public static Error StatusUnchanged(object status) => Error.Conflict("STATUS_UNCHANGED", $"Learning agreement is already {status}.");
    public static Error InvalidMode(string mode) => Error.Validation("INVALID_MODE", $"Invalid slot mode: {mode}.");
    public static Error CourseOnNonExchangeSlot => Error.Validation("INVALID_MAPPING", "Partner courses are only allowed on slots marked as AtExchange.");
    public static Error SlotNotInProfile(int slotId) => Error.Validation("SLOT_NOT_IN_PROFILE", $"Home slot {slotId} does not belong to this profile.");
    public static Error PartnerCourseNotFound(int courseId) => Error.NotFound("PARTNER_COURSE_NOT_FOUND", $"Partner course {courseId} not found.");
    public static Error EctsExceeded(int courseId, decimal available) => Error.Validation("ECTS_EXCEEDED", $"Awarded ECTS for course {courseId} exceeds available {available}.");
    public static Error RecognitionExists => Error.Conflict("RECOGNITION_EXISTS", "Cannot remove a slot mapping that has recognition data.");
    public static Error UnsupportedExportVersion => Error.Validation("INVALID_VERSION", "Unsupported export version.");
    public static Error SnapshotNotFound => Error.NotFound("SNAPSHOT_NOT_FOUND", "Snapshot not found.");
    public static Error SnapshotCorrupted => Error.Validation("INVALID_SNAPSHOT", "Snapshot data is corrupted.");
}
