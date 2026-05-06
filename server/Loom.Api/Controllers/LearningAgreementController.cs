using Loom.Application.DTOs.Exchange;
using Loom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges/{exchangeId:guid}/learning-agreement")]
[Authorize]
public class LearningAgreementController(IExchangeService exchangeService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetLearningAgreement(Guid exchangeId, CancellationToken ct)
    {
        var result = await exchangeService.GetLearningAgreementAsync(exchangeId, GetCurrentUserId(), ct);
        return Match(result, Ok);
    }

    [HttpPut]
    public async Task<IActionResult> SaveLearningAgreement(Guid exchangeId, [FromBody] SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var result = await exchangeService.SaveLearningAgreementAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }

    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus(Guid exchangeId, [FromBody] UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        var result = await exchangeService.UpdateLearningAgreementStatusAsync(exchangeId, GetCurrentUserId(), request, ct);
        return Match(result, Ok);
    }
}
