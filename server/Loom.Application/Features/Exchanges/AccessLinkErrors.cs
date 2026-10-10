using ErrorOr;

namespace Loom.Application.Features.Exchanges;

public static class AccessLinkErrors
{
    /// <summary>Same answer for unknown, revoked and already claimed links, so a token cannot be probed.</summary>
    public static Error Invalid => Error.NotFound("ACCESS_LINK_INVALID", "This access link is not valid anymore. Ask your coordinator for a new one.");
    public static Error OnlyCoordinator => Error.Forbidden("ACCESS_DENIED", "Only the exchange's coordinator can manage its access link.");
    public static Error StudentRegistered => Error.Validation("STUDENT_REGISTERED", "This student signs in with an account.");
    public static Error OnlyStudentsClaim => Error.Forbidden("CLAIM_NOT_ALLOWED", "Only student accounts can take over an exchange.");
}
