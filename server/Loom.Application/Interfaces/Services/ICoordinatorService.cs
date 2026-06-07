using ErrorOr;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.Coordinator;
using Loom.Application.DTOs.Exchange;

namespace Loom.Application.Interfaces.Services;

public interface ICoordinatorService
{
    Task<ErrorOr<List<AuthMeResponse>>> GetCoordinatorsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<CoordinatorStudentResponse>>> GetMyStudentsAsync(int coordinatorId, CancellationToken ct = default);
    Task<ErrorOr<CoordinatorStudentResponse>> CreatePlaceholderStudentAsync(int coordinatorId, CreatePlaceholderStudentRequest request, CancellationToken ct = default);
    Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(int requesterId, CancellationToken ct = default);
}
