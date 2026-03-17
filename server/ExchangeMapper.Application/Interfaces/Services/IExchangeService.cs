using ErrorOr;
using ExchangeMapper.Application.DTOs.Exchange;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IExchangeService
{
    Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(Guid studentId, CreateExchangeRequest dto, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> GetMyExchangeAsync(Guid studentId, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid studentId, Guid exchangeId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> SubmitForReviewAsync(Guid studentId, Guid exchangeId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> RetractAsync(Guid studentId, Guid exchangeId, CancellationToken ct = default);

    Task<ErrorOr<ExchangeCourseResponse>> AddCourseAsync(Guid studentId, Guid exchangeId, UpsertExchangeCourseRequest dto, CancellationToken ct = default);
    Task<ErrorOr<ExchangeCourseResponse>> UpdateCourseAsync(Guid studentId, Guid exchangeId, Guid courseId, UpsertExchangeCourseRequest dto, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> RemoveCourseAsync(Guid studentId, Guid exchangeId, Guid courseId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeCourseResponse>> UpdateGradesAsync(Guid studentId, Guid exchangeId, Guid courseId, UpdateGradesRequest dto, CancellationToken ct = default);

    Task<ErrorOr<ExchangeCourseResponse>> ProposeMappingAsync(Guid studentId, Guid exchangeId, Guid courseId, ProposeMappingRequest dto, CancellationToken ct = default);
    Task<ErrorOr<CourseMappingResponse>> ReviewMappingAsync(Guid coordinatorId, Guid exchangeId, Guid courseId, Guid mappingId, ReviewMappingRequest dto, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteMappingAsync(Guid studentId, Guid exchangeId, Guid courseId, Guid mappingId, CancellationToken ct = default);

    Task<ErrorOr<List<MappingHistoryResponse>>> GetMyHistoryAsync(Guid studentId, CancellationToken ct = default);
    Task<ErrorOr<List<MappingHistoryResponse>>> GetExchangeHistoryAsync(Guid exchangeId, CancellationToken ct = default);
    Task<ErrorOr<List<StudentExchangeSummaryResponse>>> GetStudentsWithExchangeAsync(CancellationToken ct = default);

    // Coordinator dashboard
    Task<ErrorOr<List<CoordinatorStudentSummaryResponse>>> GetCoordinatorStudentsAsync(Guid coordinatorId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> GetExchangeDetailsAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> ApproveExchangeAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> RejectExchangeAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default);
    Task<ErrorOr<ExchangeResponse>> ReturnExchangeAsync(Guid coordinatorId, Guid exchangeId, CancellationToken ct = default);

    // Mapping board
    Task<ErrorOr<MappingBoardResponse>> GetMappingBoardAsync(Guid requesterId, Guid exchangeId, CancellationToken ct = default);
    Task<ErrorOr<MappingBoardResponse>> ProposeBoardMappingAsync(Guid requesterId, Guid exchangeId, ProposeBoardMappingRequest dto, CancellationToken ct = default);
}
