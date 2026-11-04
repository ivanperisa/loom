using Loom.Domain.Common;

namespace Loom.Domain.Entities;

/// <summary>
/// A secret link that lets someone without an account work on a placeholder student's exchange.
/// The token is separate from the exchange's GUID, so the GUID can appear in URLs and logs.
/// </summary>
public class ExchangeAccessLink : EntityBase
{
    public int ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;

    /// <summary>32 random bytes, base64url. Stored as is so the coordinator can copy the link again.</summary>
    public string Token { get; set; } = null!;

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null;

    public static string NewToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
