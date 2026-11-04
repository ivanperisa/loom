using ErrorOr;

namespace Loom.Application.Features.Coordination;

public static class CoordinationErrors
{
    public static Error NotACoordinator(string action) => Error.Forbidden("FORBIDDEN", $"Only coordinators can {action}.");
    public static Error StudentNotFound => Error.NotFound("STUDENT_NOT_FOUND", "Student not found.");
    public static Error NotYourStudent(string action) => Error.Forbidden("FORBIDDEN", $"You can only {action} your own students.");
    public static Error NotAPlaceholder(string action) => Error.Validation("NOT_A_PLACEHOLDER", $"Only placeholder students can be {action} here.");
    public static Error NameRequired => Error.Validation("INVALID_NAME", "Name is required.");
    public static Error JmbagTaken => Error.Conflict("JMBAG_TAKEN", "A student with this JMBAG already exists.");
    public static Error HasExchanges => Error.Conflict("HAS_EXCHANGES", "This student has exchanges. Delete them first.");
}
