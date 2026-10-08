namespace Loom.Application.Common.Security;

/// <summary>Who is making the current request: a signed-in user, or a guest acting as a placeholder student.</summary>
public interface ICurrentActor
{
    bool IsAuthenticated { get; }

    /// <summary>The acting user's id. Throws when nobody is signed in (callers sit behind authorization).</summary>
    int UserId { get; }

    /// <summary>True when the request came in through an exchange access link instead of a login.</summary>
    bool IsGuest { get; }
}

/// <summary>Scoped; set once per request by the API (or per step by the seeder and tests).</summary>
public sealed class CurrentActor : ICurrentActor
{
    private int? _userId;

    public bool IsAuthenticated => _userId is not null;
    public int UserId => _userId ?? throw new InvalidOperationException("No current user for this request.");
    public bool IsGuest { get; private set; }

    public void Set(int userId, bool isGuest = false)
    {
        _userId = userId;
        IsGuest = isGuest;
    }
}
