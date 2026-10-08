namespace Loom.Application.Helpers;

public static class UserSyncCache
{
    public static string Key(string externalId) => $"usersync:{externalId}";
}
