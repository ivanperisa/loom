using ErrorOr;

namespace Loom.Application.Common.Errors;

/// <summary>Errors shared by several features. Codes are part of the API contract (the client maps them).</summary>
public static class CommonErrors
{
    public static Error AccessDenied => Error.Forbidden("ACCESS_DENIED", "Access denied.");
    public static Error ExchangeNotFound => Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
    public static Error UserNotFound => Error.NotFound("USER_NOT_FOUND", "User not found.");
    public static Error InstitutionNotFound => Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
    public static Error NotAHomeInstitution => Error.Validation("INVALID_INSTITUTION", "Must select a home institution.");
    public static Error CoordinatorNotFound => Error.NotFound("COORDINATOR_NOT_FOUND", "Coordinator not found.");
    public static Error InvalidJmbag => Error.Validation("INVALID_JMBAG", "JMBAG must be exactly 10 digits.");
}
