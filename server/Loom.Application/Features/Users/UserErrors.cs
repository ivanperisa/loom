using ErrorOr;

namespace Loom.Application.Features.Users;

public static class UserErrors
{
    public static Error AlreadyOnboarded => Error.Conflict("ALREADY_ONBOARDED", "User is already onboarded.");
    public static Error JmbagTaken => Error.Conflict("JMBAG_TAKEN", "This JMBAG is already in use.");
    public static Error NameRequired => Error.Validation("INVALID_NAME", "Name is required.");
    public static Error NotAStudent => Error.Conflict("ALREADY_COORDINATOR", "Only students can request coordinator access.");
    public static Error RequestPending => Error.Conflict("REQUEST_ALREADY_PENDING", "A coordinator request is already pending.");
}
