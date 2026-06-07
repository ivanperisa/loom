using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges/{exchangeGuid:guid}/learning-agreement")]
[Authorize]
public class LearningAgreementController(ILearningAgreementService learningAgreementService) : ApiController
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
}
