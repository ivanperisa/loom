namespace ExchangeMapper.Application.DTOs.Exchange;

public record UpdateExchangeStatusRequest(string Status, string? Message = null);
