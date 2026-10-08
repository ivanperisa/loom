using Loom.Api.Filters;
using Loom.Application.Features.Documents.Official;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[Route("api/exchanges/{exchangeGuid:guid}/documents")]
[AllowGuest]
[ExchangeActor]
public class DocumentController(OfficialDocumentService documents) : ApiController
{
    /// <summary>The official document (xlsx): LA with amendments, agreed recognition, results and mapping scheme, signatures.</summary>
    [HttpGet("official")]
    public async Task<IActionResult> Official(Guid exchangeGuid, [FromQuery] string? lang, CancellationToken ct) =>
        Match(await documents.BuildAsync(exchangeGuid, lang, ct), file => File(
            file.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.FileName));
}
