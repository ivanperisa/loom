using ErrorOr;
using Loom.Application.DTOs.Exchange;

namespace Loom.Application.Interfaces.Services;

public interface IExchangeService
{
    Task<ErrorOr<ExchangeResponse>> GetExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> GetPublicExchangeAsync(Guid exchangeGuid, CancellationToken ct = default);
    Task<ErrorOr<int>> ResolveGuestStudentIdAsync(Guid exchangeGuid, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyExchangesAsync(int studentId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(int requesterId, CreateExchangeRequest request, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeGuid, int requesterId, string? message, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> UpdateEwpLinkAsync(Guid exchangeGuid, int? requesterId, string? ewpLink, CancellationToken ct = default);
}
