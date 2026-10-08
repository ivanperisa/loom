namespace Loom.Application.Common.Security;

/// <summary>Who is making the current request: a signed-in user, or a guest acting as a placeholder student.</summary>
public interface ICurrentActor
{
    bool IsAuthenticated { get; }

    /// <summary>The acting user's id. Throws when nobody is signed in (callers sit behind authorization).</summary>
    int UserId { get; }

    /// <summary>True when the request came in through an access link instead of a login.</summary>
    bool IsGuest { get; }

    /// <summary>For guests: the one exchange their access link opens.</summary>
    int? GuestExchangeId { get; }
}

/// <summary>Scoped; set once per request by the API (or per step by the seeder and tests).</summary>
public sealed class CurrentActor : ICurrentActor
{
    private int? _userId;

    public bool IsAuthenticated => _userId is not null;
    public int UserId => _userId ?? throw new InvalidOperationException("No current user for this request.");
    public bool IsGuest => GuestExchangeId is not null;
    public int? GuestExchangeId { get; private set; }

    public void Set(int userId)
    {
        _userId = userId;
        GuestExchangeId = null;
    }

    public void SetGuest(int placeholderStudentId, int exchangeId)
    {
        _userId = placeholderStudentId;
        GuestExchangeId = exchangeId;
    }
}
