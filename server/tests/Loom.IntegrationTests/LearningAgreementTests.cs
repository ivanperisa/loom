using Loom.Application.Features.Documents;
using Loom.Application.Features.Planning;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.IntegrationTests;

/// <summary>The learning agreement: statuses, numbered versions and amendment marks, locks, restore, import.</summary>
public class LearningAgreementTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private sealed record Setup(int Student, int Coordinator, int Partner, Guid Exchange, Dictionary<string, int> Courses);

    private async Task<Setup> ExchangeWithLa()
    {
        var student = await NewUser();
        var coordinator = await NewUser(UserRole.Coordinator);
        var (partner, courses) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 5), AtExchange(Slot2, courses["B"], 5));
        return new Setup(student, coordinator, partner, exchange, courses);
    }

    private Task<DocumentStatus> LaStatus(Guid exchange) =>
        Db(db => db.LearningAgreements.Where(l => l.Exchange.Guid == exchange).Select(l => l.Status).SingleAsync(Ct));

    private Task<LearningAgreementResponse> GetLa(Setup s) =>
        Ok<LearningAgreementService, LearningAgreementResponse>(x => x.GetAsync(s.Exchange, Ct), actor: s.Student);

    private Task<List<DocumentVersionResponse>> Versions(Setup s) =>
        Ok<LaVersionService, List<DocumentVersionResponse>>(x => x.ListAsync(s.Exchange, Ct), actor: s.Student);

    [Fact]
    public async Task Only_the_assigned_coordinator_changes_la_status()
    {
        var s = await ExchangeWithLa();
        var otherCoordinator = await NewUser(UserRole.Coordinator);

        Assert.Equal("FORBIDDEN", (await SetLaStatus(s.Exchange, s.Student, "Approved")).FirstError.Code);
        Assert.Equal("ACCESS_DENIED", (await SetLaStatus(s.Exchange, otherCoordinator, "Approved")).FirstError.Code);
        Assert.False((await SetLaStatus(s.Exchange, s.Coordinator, "Approved")).IsError);
        Assert.Equal("FORBIDDEN", (await SetLaStatus(s.Exchange, s.Student, "Draft")).FirstError.Code);
        Assert.Equal(DocumentStatus.Approved, await LaStatus(s.Exchange));
    }

    [Theory]
    [InlineData("Submitted")]
    [InlineData("Rejected")]
    [InlineData("Nonsense")]
    [InlineData("5")]
    public async Task Only_draft_and_approved_are_valid_statuses(string status)
    {
        var s = await ExchangeWithLa();
        Assert.Equal("INVALID_STATUS", (await SetLaStatus(s.Exchange, s.Coordinator, status)).FirstError.Code);
    }

    [Fact]
    public async Task Each_approval_with_changes_is_the_next_version_and_marks_its_amendments()
    {
        var s = await ExchangeWithLa();
        await Approve(s.Exchange, s.Coordinator);                                   // v1: A, B
        await Reopen(s.Exchange, s.Coordinator);
        await SaveLa(s.Exchange, s.Student, AtExchange(Slot1, s.Courses["A"], 5), AtExchange(Slot3, s.Courses["C"], 4));
        await Approve(s.Exchange, s.Coordinator);                                   // v2 = A1: −B, +C

        var la = await GetLa(s);
        Assert.Equal(2, la.SignedCount);
        var byCourse = la.Entries.ToDictionary(e => e.PartnerCourseId!.Value);
        Assert.Equal((false, 0), (byCourse[s.Courses["A"]].IsDeleted, byCourse[s.Courses["A"]].AmendmentNumber));
        Assert.Equal((true, 1), (byCourse[s.Courses["B"]].IsDeleted, byCourse[s.Courses["B"]].AmendmentNumber));
        Assert.Equal((false, 1), (byCourse[s.Courses["C"]].IsDeleted, byCourse[s.Courses["C"]].AmendmentNumber));

        var versions = await Versions(s);
        Assert.Equal([2, 1], versions.Select(v => v.VersionNo));
        Assert.Equal("A1", versions[0].AmendmentLabel);
        Assert.Equal(
            [("Removed", "B"), ("Added", "C")],
            versions[0].Changes!.Select(c => (c.Type, c.PartnerCourseCode!)).OrderBy(c => c.Item2).ToList());
        Assert.Equal(2, versions[1].Changes!.Count(c => c.Type == DocumentDiff.Added));   // the original against nothing
    }

    [Fact]
    public async Task Approving_again_without_changes_keeps_the_version_number()
    {
        var s = await ExchangeWithLa();
        await Approve(s.Exchange, s.Coordinator);
        await Reopen(s.Exchange, s.Coordinator);
        await Approve(s.Exchange, s.Coordinator);

        Assert.Single(await Versions(s));
        Assert.Equal(1, (await GetLa(s)).SignedCount);
    }

    [Fact]
    public async Task Taking_out_an_approved_course_strikes_it_through_and_a_new_one_just_disappears()
    {
        var s = await ExchangeWithLa();
        await Approve(s.Exchange, s.Coordinator);
        await Reopen(s.Exchange, s.Coordinator);

        await SaveLa(s.Exchange, s.Student, AtExchange(Slot1, s.Courses["A"], 5), AtExchange(Slot3, s.Courses["D"], 5));
        await SaveLa(s.Exchange, s.Student, AtExchange(Slot1, s.Courses["A"], 5));
        var entries = (await GetLa(s)).Entries;
        var removed = Assert.Single(entries, e => e.IsDeleted);
        Assert.Equal((s.Courses["B"], (int?)null), (removed.PartnerCourseId!.Value, removed.AmendmentNumber));   // pending: next amendment
        Assert.DoesNotContain(entries, e => e.PartnerCourseId == s.Courses["D"]);

        // Putting it back un-marks the same row instead of adding a second one.
        await SaveLa(s.Exchange, s.Student, AtExchange(Slot1, s.Courses["A"], 5), AtExchange(Slot2, s.Courses["B"], 5));
        Assert.Equal(2, (await GetLa(s)).Entries.Count(e => !e.IsDeleted));
        Assert.DoesNotContain((await GetLa(s)).Entries, e => e.IsDeleted);
    }

    [Fact]
    public async Task Saving_validates_courses_ects_and_duplicates()
    {
        var s = await ExchangeWithLa();
        var (_, foreignCourses) = await NewPartner();
        async Task<string> Error(params LearningAgreementEntryUpsertDto[] entries) =>
            (await Call<LearningAgreementService, LearningAgreementResponse>(x =>
                x.SaveAsync(s.Exchange, new SaveLearningAgreementRequest([.. entries]), Ct), actor: s.Student)).FirstError.Code;

        Assert.Equal("COURSE_NOT_AT_PARTNER", await Error(AtExchange(Slot1, foreignCourses["A"], 5)));
        Assert.Equal("DUPLICATE_ENTRY", await Error(AtExchange(Slot1, s.Courses["A"], 3), AtExchange(Slot1, s.Courses["A"], 2)));
        Assert.Equal("ECTS_REQUIRED", await Error(new LearningAgreementEntryUpsertDto(Slot1, "AtExchange", s.Courses["A"], null)));
        Assert.Equal("ECTS_EXCEEDED", await Error(AtExchange(Slot1, s.Courses["A"], 4), AtExchange(Slot2, s.Courses["A"], 4)));
        Assert.Equal("MIXED_SLOT_MODES", await Error(AtExchange(Slot1, s.Courses["A"], 3), new LearningAgreementEntryUpsertDto(Slot1, "AtHome", null, null)));
    }

    [Fact]
    public async Task Approved_la_cannot_be_saved_imported_or_restored()
    {
        var s = await ExchangeWithLa();
        var export = await Ok<LaTransferService, MappingExportDto>(x => x.ExportAsync(s.Exchange, Ct), actor: s.Student);
        await Approve(s.Exchange, s.Coordinator);
        var version = (await Versions(s)).Single();

        var save = await Call<LearningAgreementService, LearningAgreementResponse>(x =>
            x.SaveAsync(s.Exchange, new SaveLearningAgreementRequest([AtExchange(Slot1, s.Courses["C"], 5)]), Ct), actor: s.Student);
        var import = await Call<LaTransferService, ImportResult>(x => x.ApplyAsync(s.Exchange, export, Ct), actor: s.Student);
        var preview = await Ok<LaTransferService, ImportPreviewResponse>(x => x.PreviewAsync(s.Exchange, export, Ct), actor: s.Student);
        var restore = await Call<LaVersionService, RestoreResult>(x => x.RestoreAsync(s.Exchange, version.Id, Ct), actor: s.Student);

        Assert.Equal("LA_LOCKED", save.FirstError.Code);
        Assert.Equal("LA_LOCKED", import.FirstError.Code);
        Assert.Equal(("LA_LOCKED", false), (preview.BlockingCode, preview.CanApply));
        Assert.Equal("LA_LOCKED", restore.FirstError.Code);
        Assert.Equal(DocumentStatus.Approved, await LaStatus(s.Exchange));
    }

    [Fact]
    public async Task Restore_keeps_the_status_backs_up_the_draft_and_reports_courses_that_are_gone()
    {
        var s = await ExchangeWithLa();
        await Approve(s.Exchange, s.Coordinator);                                   // v1: A, B
        await Reopen(s.Exchange, s.Coordinator);
        await SaveLa(s.Exchange, s.Student, AtExchange(Slot1, s.Courses["C"], 5));
        var v1 = (await Versions(s)).Single();
        await Db(db => db.Database.ExecuteSqlAsync($"DELETE FROM partner.course WHERE id = {s.Courses["B"]}", Ct));

        var result = await Ok<LaVersionService, RestoreResult>(x => x.RestoreAsync(s.Exchange, v1.Id, Ct), actor: s.Student);

        Assert.Equal(DocumentStatus.Draft, await LaStatus(s.Exchange));
        Assert.Equal(1, result.Entries);
        Assert.Equal(("CourseNotFound", "B"), (Assert.Single(result.Missing).Reason, result.Missing[0].PartnerCourseCode));
        Assert.Equal([s.Courses["A"]], (await GetLa(s)).Entries.Where(e => !e.IsDeleted).Select(e => e.PartnerCourseId!.Value));
        var backup = Assert.Single(await Versions(s), v => v.Kind == VersionKind.Backup);
        Assert.Equal(1, backup.EntryCount);   // the draft with C

        // Restoring the same content again changes nothing and takes no second backup.
        await Ok<LaVersionService, RestoreResult>(x => x.RestoreAsync(s.Exchange, v1.Id, Ct), actor: s.Student);
        Assert.Single(await Versions(s), v => v.Kind == VersionKind.Backup);
    }

    [Fact]
    public async Task Import_previews_and_replaces_the_draft_matching_courses_by_code_at_another_partner()
    {
        var source = await ExchangeWithLa();
        var export = await Ok<LaTransferService, MappingExportDto>(x => x.ExportAsync(source.Exchange, Ct), actor: source.Student);

        var target = await ExchangeWithLa();   // another partner with its own A, B, C, D
        await SaveLa(target.Exchange, target.Student, AtExchange(Slot1, target.Courses["A"], 3), AtExchange(Slot3, target.Courses["C"], 6));
        var withExtra = export with { Mappings = [.. export.Mappings, export.Mappings[0] with { HomeSlotId = 999_999 }] };

        var preview = await Ok<LaTransferService, ImportPreviewResponse>(x => x.PreviewAsync(target.Exchange, withExtra, Ct), actor: target.Student);

        Assert.True(preview.CanApply);
        Assert.Contains(preview.ContextWarnings, w => w.Field == "partnerInstitution");
        Assert.Equal(["B"], preview.Added.Select(r => r.PartnerCourseCode));
        Assert.Equal(["C"], preview.Removed.Select(r => r.PartnerCourseCode));
        Assert.Equal((3m, 5m), (preview.Changed.Single().PreviousEcts!.Value, preview.Changed.Single().AwardedEcts!.Value));
        Assert.Equal("SlotNotInProfile", Assert.Single(preview.Skipped).Reason);

        var applied = await Ok<LaTransferService, ImportResult>(x => x.ApplyAsync(target.Exchange, withExtra, Ct), actor: target.Student);
        Assert.Equal((1, 1, 1), (applied.Added, applied.Removed, applied.Changed));
        var live = (await GetLa(target)).Entries.Where(e => !e.IsDeleted).Select(e => e.PartnerCourseCode).Order();
        Assert.Equal(["A", "B"], live);
        Assert.Single(await Versions(target), v => v.Kind == VersionKind.Backup);
    }

    [Fact]
    public async Task Import_reports_ambiguous_codes_and_refuses_what_a_save_would_refuse()
    {
        var s = await ExchangeWithLa();
        var export = await Ok<LaTransferService, MappingExportDto>(x => x.ExportAsync(s.Exchange, Ct), actor: s.Student);
        var target = await ExchangeWithLa();
        await Db(async db =>
        {
            db.PartnerCourses.Add(new PartnerCourse
            {
                InstitutionId = target.Partner, Code = "a", Name = "Another A", Ects = 6,
                Semester = ExchangeSemester.Winter, Level = StudyProgramLevel.Graduate,
            });
            return await db.SaveChangesAsync(Ct);
        });

        var preview = await Ok<LaTransferService, ImportPreviewResponse>(x => x.PreviewAsync(target.Exchange, export, Ct), actor: target.Student);
        Assert.Contains(preview.Skipped, k => k is { PartnerCourseCode: "A", Reason: "AmbiguousCourse" });

        var tooMuch = export with { Mappings = export.Mappings.Select(m => m with { AwardedEcts = 50 }).ToList() };
        var blocked = await Ok<LaTransferService, ImportPreviewResponse>(x => x.PreviewAsync(s.Exchange, tooMuch, Ct), actor: s.Student);
        Assert.Equal((false, "ECTS_EXCEEDED"), (blocked.CanApply, blocked.BlockingCode));
        Assert.Equal("ECTS_EXCEEDED", (await Call<LaTransferService, ImportResult>(x => x.ApplyAsync(s.Exchange, tooMuch, Ct), actor: s.Student)).FirstError.Code);

        var notAnExport = export with { Version = 7 };
        Assert.Equal("INVALID_IMPORT_FILE", (await Call<LaTransferService, ImportPreviewResponse>(x => x.PreviewAsync(s.Exchange, notAnExport, Ct), actor: s.Student)).FirstError.Code);
    }
}
