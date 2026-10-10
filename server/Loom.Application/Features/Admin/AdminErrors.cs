using ErrorOr;

namespace Loom.Application.Features.Admin;

public static class AdminErrors
{
    public static Error JmbagTaken => Error.Conflict("JMBAG_TAKEN", "A student with this JMBAG already exists.");
    public static Error CannotChangeOwnRole => Error.Validation("CANNOT_CHANGE_OWN_ROLE", "You cannot change your own role.");
    public static Error NoPendingRequest => Error.Validation("NO_PENDING_REQUEST", "User does not have a pending coordinator request.");
    public static Error InvalidDecision => Error.Validation("INVALID_STATUS", "A coordinator request is either Approved or Rejected.");
    public static Error EmailRequired => Error.Validation("INVALID_EMAIL", "Email is required.");
    public static Error AlreadyWhitelisted => Error.Conflict("EMAIL_ALREADY_WHITELISTED", "This email is already on the coordinator whitelist.");
    public static Error NotWhitelisted => Error.NotFound("EMAIL_NOT_FOUND", "Email not found on the coordinator whitelist.");
}
