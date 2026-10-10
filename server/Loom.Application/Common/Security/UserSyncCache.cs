namespace Loom.Application.Common.Security;

public static class UserSyncCache
{
    public static string Key(string externalId) => $"usersync:{externalId}";
}
