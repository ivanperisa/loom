using ExchangeMapper.Domain.Common;

namespace ExchangeMapper.Domain.Entities;

public class CoordinatorWhitelist : EntityBase
{
    public string Email { get; set; } = string.Empty;
}
