using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Domain.Entities;

public class User : EntityBase
{
    public string ExternalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsOnboarded { get; set; }

    public ICollection<UserInstitution> UserInstitutions { get; set; } = [];
    public ICollection<Exchange> StudentExchanges { get; set; } = [];
    public ICollection<MappingHistory> MappingHistoryChanges { get; set; } = [];
}
