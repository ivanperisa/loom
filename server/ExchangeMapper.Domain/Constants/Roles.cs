using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Constants;

public static class Roles
{
    public const string Student = nameof(UserRole.Student);
    public const string Coordinator = nameof(UserRole.Coordinator);
    public const string Admin = nameof(UserRole.Admin);
}
