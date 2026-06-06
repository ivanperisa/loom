using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("exchanges/{exchangeGuid:guid}/learning-agreement")]
[Authorize]
public class LearningAgreementController(IExchangeService exchangeService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetLearningAgreement(Guid exchangeGuid, CancellationToken ct)
    {
        var result = await exchangeService.GetLearningAgreementAsync(exchangeGuid, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut]
    public async Task<IActionResult> SaveLearningAgreement(Guid exchangeGuid, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var result = await exchangeService.SaveLearningAgreementAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeGuid, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        var result = await exchangeService.UpdateLearningAgreementStatusAsync(exchangeGuid, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }
}
