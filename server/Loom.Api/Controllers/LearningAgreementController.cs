using Loom.Api.Extensions;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges/{exchangeGuid:guid}/learning-agreement")]
[Authorize]
public class LearningAgreementController(ILearningAgreementService learningAgreementService, IExchangeService exchangeService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetLearningAgreement(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await learningAgreementService.GetLearningAgreementAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut]
    public async Task<IActionResult> SaveLearningAgreement(Guid exchangeGuid, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var result = await learningAgreementService.SaveLearningAgreementAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeGuid, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        var result = await learningAgreementService.UpdateLearningAgreementStatusAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpGet("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement")]
    public async Task<IActionResult> GetPublicLearningAgreement(Guid exchangeGuid, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.GetLearningAgreementAsync(exchangeGuid, studentIdResult.Value, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPut("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement")]
    public async Task<IActionResult> SavePublicLearningAgreement(Guid exchangeGuid, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.SaveLearningAgreementAsync(exchangeGuid, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }

    [AllowAnonymous]
    [HttpPatch("/api/exchanges/access/{exchangeGuid:guid}/learning-agreement/status")]
    public async Task<IActionResult> UpdatePublicStatus(Guid exchangeGuid, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        var studentIdResult = await exchangeService.ResolveGuestStudentIdAsync(exchangeGuid, ct);
        if (studentIdResult.IsError) return studentIdResult.Errors.ToProblemDetails(this);

        var result = await learningAgreementService.UpdateLearningAgreementStatusAsync(exchangeGuid, studentIdResult.Value, request, ct);
        return Match(result, Ok);
    }
}
