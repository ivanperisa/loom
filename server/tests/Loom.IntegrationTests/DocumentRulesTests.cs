using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.DTOs.Recognition;
using Loom.Application.Interfaces.Services;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.IntegrationTests;

/// <summary>Status changes and locks on the learning agreement and recognition.</summary>
public class DocumentRulesTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private async Task<(int Student, int Coordinator, Guid Exchange, Dictionary<string, int> Courses)> ExchangeWithLa()
    {
        var student = await NewUser();
        var coordinator = await NewUser(UserRole.Coordinator);
        var (partner, courses) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 5), AtExchange(Slot2, courses["B"], 5));
        return (student, coordinator, exchange, courses);
    }

    private Task<DocumentStatus> LaStatus(Guid exchange) =>
        Db(db => db.LearningAgreements.Where(l => l.Exchange.Guid == exchange).Select(l => l.Status).SingleAsync(Ct));

    [Fact]
    public async Task Only_the_assigned_coordinator_changes_la_status()
    {
        var (student, coordinator, exchange, _) = await ExchangeWithLa();
        var otherCoordinator = await NewUser(UserRole.Coordinator);

        Assert.Equal("FORBIDDEN", (await SetLaStatus(exchange, student, "Approved")).FirstError.Code);
        Assert.Equal("FORBIDDEN", (await SetLaStatus(exchange, otherCoordinator, "Approved")).FirstError.Code);
        Assert.False((await SetLaStatus(exchange, coordinator, "Approved")).IsError);
        Assert.Equal("FORBIDDEN", (await SetLaStatus(exchange, student, "Draft")).FirstError.Code);
        Assert.Equal(DocumentStatus.Approved, await LaStatus(exchange));
    }

    [Theory]
    [InlineData("Submitted")]
    [InlineData("Rejected")]
    [InlineData("Nonsense")]
    public async Task Only_draft_and_approved_are_valid_statuses(string status)
    {
        var (_, coordinator, exchange, _) = await ExchangeWithLa();
        Assert.Equal("INVALID_STATUS", (await SetLaStatus(exchange, coordinator, status)).FirstError.Code);
    }

    [Fact]
    public async Task Approving_twice_is_a_conflict_and_creates_one_snapshot()
    {
        var (_, coordinator, exchange, _) = await ExchangeWithLa();
        Assert.False((await SetLaStatus(exchange, coordinator, "Approved")).IsError);
        Assert.Equal("STATUS_UNCHANGED", (await SetLaStatus(exchange, coordinator, "Approved")).FirstError.Code);
        Assert.Equal(1, await Db(db => db.ExchangeSnapshots.CountAsync(s => s.Exchange.Guid == exchange, Ct)));
    }

    [Fact]
    public async Task Approved_la_cannot_be_saved_imported_or_restored()
    {
        var (student, coordinator, exchange, courses) = await ExchangeWithLa();
        var export = await Ok<ILearningAgreementService, MappingExportDto>(s => s.ExportMappingsAsync(exchange, student, Ct));
        Assert.False((await SetLaStatus(exchange, coordinator, "Approved")).IsError);
        var snapshotId = await Db(db => db.ExchangeSnapshots.Where(s => s.Exchange.Guid == exchange).Select(s => s.Id).SingleAsync(Ct));

        var save = await Call<ILearningAgreementService, LearningAgreementResponse>(s =>
            s.SaveLearningAgreementAsync(exchange, student, new SaveLearningAgreementRequest([AtExchange(Slot1, courses["C"], 5)]), Ct));
        var import = await Call<ILearningAgreementService, MappingImportResult>(s => s.ImportMappingsAsync(exchange, student, export, Ct));
        var restore = await Call<ILearningAgreementService, ErrorOr.Updated>(s => s.RestoreSnapshotAsync(exchange, snapshotId, student, Ct));

        Assert.Equal("LA_LOCKED", save.FirstError.Code);
        Assert.Equal("LA_LOCKED", import.FirstError.Code);
        Assert.Equal("LA_LOCKED", restore.FirstError.Code);
        Assert.Equal(DocumentStatus.Approved, await LaStatus(exchange));
    }

    [Fact]
    public async Task Restore_in_draft_keeps_the_status_and_creates_a_backup()
    {
        var (student, coordinator, exchange, courses) = await ExchangeWithLa();
        Assert.False((await SetLaStatus(exchange, coordinator, "Approved")).IsError);
        Assert.False((await SetLaStatus(exchange, coordinator, "Draft")).IsError);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["C"], 5));
        var approved = await Db(db => db.ExchangeSnapshots.Where(s => s.Exchange.Guid == exchange).Select(s => s.Id).SingleAsync(Ct));

        await Ok<ILearningAgreementService, ErrorOr.Updated>(s => s.RestoreSnapshotAsync(exchange, approved, student, Ct));

        Assert.Equal(DocumentStatus.Draft, await LaStatus(exchange));
        var entries = await Db(db => db.LearningAgreementEntries.Where(e => e.LearningAgreement.Exchange.Guid == exchange)
            .Select(e => e.PartnerCourseId).ToListAsync(Ct));
        Assert.Equivalent(new int?[] { courses["A"], courses["B"] }, entries);
        Assert.Equal(1, await Db(db => db.ExchangeSnapshots.CountAsync(s => s.Exchange.Guid == exchange && s.Type == SnapshotType.PreImport, Ct)));
    }

    [Fact]
    public async Task Import_runs_the_normal_save_validation()
    {
        var (student, _, exchange, _) = await ExchangeWithLa();
        var export = await Ok<ILearningAgreementService, MappingExportDto>(s => s.ExportMappingsAsync(exchange, student, Ct));
        var invalid = export with { Mappings = export.Mappings.Select(m => m with { Mode = "Bogus", PartnerCourse = null }).ToList() };

        var result = await Call<ILearningAgreementService, MappingImportResult>(s => s.ImportMappingsAsync(exchange, student, invalid, Ct));

        Assert.Equal("INVALID_MODE", result.FirstError.Code);
    }

    [Fact]
    public async Task Recognition_save_rejects_entries_from_another_exchange()
    {
        var (student, _, exchange, _) = await ExchangeWithLa();
        var (otherStudent, _, otherExchange, _) = await ExchangeWithLa();
        var foreignEntry = await Db(db => db.LearningAgreementEntries
            .Where(e => e.LearningAgreement.Exchange.Guid == otherExchange).Select(e => e.Id).FirstAsync(Ct));
        await Ok<IRecognitionService, RecognitionResponse>(s => s.GetOrCreateRecognitionAsync(exchange, student, Ct));

        var result = await Call<IRecognitionService, RecognitionResponse>(s => s.SaveRecognitionAsync(exchange, student,
            new SaveRecognitionRequest([new UpsertRecognitionEntryRequest(foreignEntry, "Passed", "9", "A", "5", null)]), Ct));

        Assert.Equal("ENTRY_NOT_FOUND", result.FirstError.Code);
        Assert.NotEqual(student, otherStudent);
    }

    [Fact]
    public async Task Only_the_assigned_coordinator_changes_recognition_status()
    {
        var (student, coordinator, exchange, _) = await ExchangeWithLa();
        await Ok<IRecognitionService, RecognitionResponse>(s => s.GetOrCreateRecognitionAsync(exchange, student, Ct));

        var byStudent = await Call<IRecognitionService, RecognitionResponse>(s =>
            s.UpdateRecognitionStatusAsync(exchange, student, new UpdateRecognitionStatusRequest("Approved"), Ct));
        var submitted = await Call<IRecognitionService, RecognitionResponse>(s =>
            s.UpdateRecognitionStatusAsync(exchange, coordinator, new UpdateRecognitionStatusRequest("Submitted"), Ct));

        Assert.Equal("FORBIDDEN", byStudent.FirstError.Code);
        Assert.Equal("INVALID_STATUS", submitted.FirstError.Code);
    }
}
