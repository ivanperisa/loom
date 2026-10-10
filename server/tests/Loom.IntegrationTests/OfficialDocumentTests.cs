using ClosedXML.Excel;
using Loom.Application.Features.Documents.Official;
using Loom.Domain.Enums;

namespace Loom.IntegrationTests;

public class OfficialDocumentTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task The_la_sheet_shows_the_approved_version_while_a_reopened_draft_changes_it()
    {
        var student = await NewUser();
        var coordinator = await NewUser(UserRole.Coordinator);
        var (partner, courses) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 5));
        await Approve(exchange, coordinator);
        await Reopen(exchange, coordinator);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 2));

        var file = await Ok<OfficialDocumentService, OfficialDocumentFile>(x => x.BuildAsync(exchange, "en", Ct), actor: student);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        var texts = workbook.Worksheets.SelectMany(ws => ws.CellsUsed()).Select(c => c.GetString()).ToList();
        Assert.Contains(texts, t => t.EndsWith("\n5 ECTS"));
        Assert.DoesNotContain(texts, t => t.EndsWith("\n2 ECTS"));
    }
}
