using Loom.Application.Features.Completion;
using Loom.Application.Features.Documents;
using Loom.Application.Features.Planning;
using Loom.Domain.Enums;

namespace Loom.IntegrationTests;

/// <summary>"Start final recognition", table 1 from the approved LA, results (grades + mapping scheme), approval.</summary>
public class RecognitionTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private sealed record Setup(int Student, int Coordinator, Guid Exchange, Dictionary<string, int> Courses);

    /// <summary>LA with A (5) and B (6, split over two slots: 3 + 3), approved.</summary>
    private async Task<Setup> ApprovedExchange()
    {
        var student = await NewUser();
        var coordinator = await NewUser(UserRole.Coordinator);
        var (partner, courses) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 5), AtExchange(Slot2, courses["B"], 3), AtExchange(Slot3, courses["B"], 3));
        await Approve(exchange, coordinator);
        return new Setup(student, coordinator, exchange, courses);
    }

    private async Task<Setup> StartedExchange()
    {
        var s = await ApprovedExchange();
        await Ok<RecognitionService, RecognitionResponse>(x => x.StartAsync(s.Exchange, Ct), actor: s.Student);
        return s;
    }

    private Task<RecognitionResponse> Get(Setup s) =>
        Ok<RecognitionService, RecognitionResponse>(x => x.GetAsync(s.Exchange, Ct), actor: s.Student);

    private Task<MappingSchemeResponse> Scheme(Setup s) =>
        Ok<MappingSchemeService, MappingSchemeResponse>(x => x.GetAsync(s.Exchange, Ct), actor: s.Student);

    private Task<ErrorOr.ErrorOr<RecognitionResponse>> SaveGrades(Setup s, params CourseGradesRequest[] grades) =>
        Call<RecognitionService, RecognitionResponse>(x => x.SaveGradesAsync(s.Exchange, new SaveGradesRequest([.. grades]), Ct), actor: s.Student);

    private Task<ErrorOr.ErrorOr<RecognitionResponse>> SetStatus(Setup s, int actor, string status) =>
        Call<RecognitionService, RecognitionResponse>(x => x.SetStatusAsync(s.Exchange, new UpdateRecognitionStatusRequest(status), Ct), actor: actor);

    private static CourseGradesRequest Passed(int courseId, string grade = "A") => new(courseId, "Passed", "1.0", grade, "5", new DateOnly(2026, 2, 1));

    [Fact]
    public async Task Final_recognition_starts_only_from_an_approved_la_and_only_once()
    {
        var student = await NewUser();
        var coordinator = await NewUser(UserRole.Coordinator);
        var (partner, courses) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 5));

        var early = await Call<RecognitionService, RecognitionResponse>(x => x.StartAsync(exchange, Ct), actor: student);
        Assert.Equal("LA_NOT_APPROVED", early.FirstError.Code);

        await Approve(exchange, coordinator);
        var started = await Ok<RecognitionService, RecognitionResponse>(x => x.StartAsync(exchange, Ct), actor: student);
        Assert.True(started.IsStarted);
        Assert.False(started.CanStart);

        var again = await Call<RecognitionService, RecognitionResponse>(x => x.StartAsync(exchange, Ct), actor: coordinator);
        Assert.Equal("RECOGNITION_ALREADY_STARTED", again.FirstError.Code);
    }

    [Fact]
    public async Task Table_1_is_the_latest_approved_version_never_the_draft()
    {
        var s = await ApprovedExchange();
        await Reopen(s.Exchange, s.Coordinator);
        await SaveLa(s.Exchange, s.Student, AtExchange(Slot1, s.Courses["C"], 5));

        var recognition = await Get(s);

        Assert.Equal(1, recognition.AgreedVersionNo);
        Assert.Equal(["A", "B", "B"], recognition.Agreed.Select(e => e.PartnerCourseCode).Order());
        Assert.False(recognition.CanStart);   // the LA is a draft again
    }

    [Fact]
    public async Task Starting_freezes_the_la_for_good_and_copies_the_approved_version_into_the_results()
    {
        var s = await StartedExchange();

        var scheme = await Scheme(s);
        Assert.Equal([(Slot1, 5m), (Slot2, 3m), (Slot3, 3m)], scheme.Entries.Select(e => (e.HomeSlotId, e.AwardedEcts)).Order());

        var save = await Call<LearningAgreementService, LearningAgreementResponse>(x =>
            x.SaveAsync(s.Exchange, new SaveLearningAgreementRequest([AtExchange(Slot1, s.Courses["A"], 5)]), Ct), actor: s.Student);
        Assert.Equal("LA_CONCLUDED", save.FirstError.Code);
        Assert.Equal("LA_CONCLUDED", (await SetLaStatus(s.Exchange, s.Coordinator, "Draft")).FirstError.Code);
        Assert.True((await Ok<LearningAgreementService, LearningAgreementResponse>(x => x.GetAsync(s.Exchange, Ct), actor: s.Student)).IsConcluded);
    }

    [Fact]
    public async Task Grades_need_a_started_recognition_and_apply_to_every_slot_of_the_course()
    {
        var s = await ApprovedExchange();
        Assert.Equal("RECOGNITION_NOT_STARTED", (await SaveGrades(s, Passed(s.Courses["A"]))).FirstError.Code);

        await Ok<RecognitionService, RecognitionResponse>(x => x.StartAsync(s.Exchange, Ct), actor: s.Student);
        Assert.False((await SaveGrades(s, Passed(s.Courses["B"], "B"))).IsError);

        var b = (await Scheme(s)).Entries.Where(e => e.PartnerCourseId == s.Courses["B"]).ToList();
        Assert.Equal(2, b.Count);
        Assert.All(b, e => Assert.Equal((EnrollmentStatus.Passed, "B"), (e.EnrollmentStatus, e.EctsGrade)));

        Assert.Equal("INVALID_ENROLLMENT_STATUS", (await SaveGrades(s, new CourseGradesRequest(s.Courses["A"], "Maybe", null, null, null, null))).FirstError.Code);
        Assert.Equal("INVALID_PARTNER_COURSE", (await SaveGrades(s, Passed(s.Courses["D"]))).FirstError.Code);
        Assert.Equal("INVALID_GRADE", (await SaveGrades(s, Passed(s.Courses["A"], "TOOLONG"))).FirstError.Code);
    }

    [Fact]
    public async Task The_mapping_scheme_moves_and_splits_but_never_creates_ects()
    {
        var s = await StartedExchange();
        var scheme = await Scheme(s);
        var a = scheme.Entries.Single(e => e.PartnerCourseId == s.Courses["A"]);
        SaveMappingSchemeEntryRequest Keep(MappingSchemeEntryResponse e, int? slot = null, decimal? ects = null) =>
            new(e.Id, slot ?? e.HomeSlotId, e.PartnerCourseId, ects ?? e.AwardedEcts, e.EnrollmentStatus?.ToString(), e.OriginalGrade, e.EctsGrade, e.HrGrade, e.ExamDate);
        var others = scheme.Entries.Where(e => e != a).Select(e => Keep(e)).ToList();
        Task<ErrorOr.ErrorOr<MappingSchemeResponse>> Save(params SaveMappingSchemeEntryRequest[] entries) =>
            Call<MappingSchemeService, MappingSchemeResponse>(x => x.SaveAsync(s.Exchange, new SaveMappingSchemeRequest([.. others, .. entries]), Ct), actor: s.Student);

        // Split A (6 ECTS available): 5 in slot 3 + 1 in slot 1 is fine.
        var split = new SaveMappingSchemeEntryRequest(0, Slot1, a.PartnerCourseId, 1, null, null, null, null, null);
        Assert.False((await Save(Keep(a, slot: Slot3), split)).IsError);
        Assert.Equal(2, (await Scheme(s)).Entries.Count(e => e.PartnerCourseId == s.Courses["A"]));

        var refreshed = (await Scheme(s)).Entries;
        a = refreshed.First(e => e.PartnerCourseId == s.Courses["A"]);
        others = refreshed.Where(e => e != a).Select(e => Keep(e)).ToList();
        Assert.Equal("ECTS_EXCEEDED", (await Save(Keep(a, ects: 6))).FirstError.Code);
        Assert.Equal("SLOT_NOT_IN_PROFILE", (await Save(Keep(a, slot: 999_999))).FirstError.Code);
        Assert.Equal("INVALID_PARTNER_COURSE", (await Save(Keep(a), new SaveMappingSchemeEntryRequest(0, Slot1, s.Courses["D"], 1, null, null, null, null, null))).FirstError.Code);
    }

    [Fact]
    public async Task The_coordinator_approves_results_as_versions_and_approval_locks_them()
    {
        var s = await StartedExchange();
        await SaveGrades(s, Passed(s.Courses["A"]), Passed(s.Courses["B"]));

        Assert.Equal("FORBIDDEN", (await SetStatus(s, s.Student, "Approved")).FirstError.Code);
        Assert.False((await SetStatus(s, s.Coordinator, "Approved")).IsError);

        Assert.Equal("RECOGNITION_LOCKED", (await SaveGrades(s, Passed(s.Courses["A"], "B"))).FirstError.Code);
        var lockedScheme = await Call<MappingSchemeService, MappingSchemeResponse>(x => x.SaveAsync(s.Exchange, new SaveMappingSchemeRequest([]), Ct), actor: s.Student);
        Assert.Equal("RECOGNITION_LOCKED", lockedScheme.FirstError.Code);

        // Reopen without changes and approve again: still one version. Change a grade: version 2 shows it.
        Assert.False((await SetStatus(s, s.Coordinator, "Draft")).IsError);
        Assert.False((await SetStatus(s, s.Coordinator, "Approved")).IsError);
        Assert.False((await SetStatus(s, s.Coordinator, "Draft")).IsError);
        Assert.False((await SaveGrades(s, Passed(s.Courses["A"], "C"))).IsError);
        Assert.False((await SetStatus(s, s.Coordinator, "Approved")).IsError);

        var versions = await Ok<RecognitionService, List<DocumentVersionResponse>>(x => x.ListVersionsAsync(s.Exchange, Ct), actor: s.Student);
        Assert.Equal([2, 1], versions.Select(v => v.VersionNo));
        var change = Assert.Single(versions[0].Changes!);
        Assert.Equal((DocumentDiff.Modified, "A"), (change.Type, change.PartnerCourseCode));
        Assert.Equal(("ectsGrade", "A", "C"), (change.Fields.Single().Field, change.Fields.Single().Before, change.Fields.Single().After));
        Assert.Equal(2, (await Get(s)).ApprovedVersionCount);
    }
}
