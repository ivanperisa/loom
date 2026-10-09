using ErrorOr;
using Loom.Application.Features.Planning;
using Loom.Application.Features.Exchanges;
using Loom.Application.Features.Completion;
using Loom.Application.Features.Documents;
using Loom.Application.Features.Admin;
using Loom.Application.Features.Coordination;
using Loom.Application.Features.Users;
using Loom.Application.Common.Security;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Loom.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Loom.DevSeed;

/// <summary>
/// Demo scenarios for local testing. Users, catalogue rows and a few flags are written directly;
/// everything with business rules (exchanges, learning agreements, approvals, recognition, mapping scheme)
/// goes through the real application services, so the data looks exactly like the app would produce it.
/// The persona table in README.md documents what each account is for.
/// </summary>
public sealed class DemoData(IServiceProvider services, ILogger log)
{
    private const int HomeInstitutionId = 1;   // FER, from reference-data.sql
    private const int ProfileId = 101;         // "Programsko inženjerstvo i informacijski sustavi"
    private const string Year = "2025/2026";

    // Semester 3 slots of profile 101 (see reference-data.sql).
    private const int SlotElective1 = 214, SlotElective2 = 215, SlotElective3 = 216;
    private const int SlotFree1 = 217, SlotFree2 = 218, SlotSeminar = 219, SlotTransversal = 220;

    private int _adminId;
    private readonly Dictionary<string, int> _courses = new();   // partner course code -> id (TUM)
    private readonly Dictionary<string, int> _partners = new();  // short key -> institution id

    /// <summary>Stable exchange GUIDs, so links in the README keep working after a reset.</summary>
    public static Guid ExchangeGuid(int n) => new($"00000000-0000-4000-8000-{n:D12}");

    /// <summary>Stable access-link tokens for the placeholder exchanges (real ones are random).</summary>
    public static string AccessToken(int n) => $"dev-access-link-{n:D2}";

    public async Task SeedAsync()
    {
        await SeedCatalogAsync();
        await SeedAccountsAsync();
        await SeedExchangesAsync();
        await SeedVolumeAsync();
    }

    // ---------------------------------------------------------------- catalogue

    private async Task SeedCatalogAsync()
    {
        await WithDb(async db =>
        {
            var tum = Partner("Technische Universität München", "Tehničko sveučilište u Münchenu", "Germany", "München", "D  MUNCHEN02");
            var polimi = Partner("Politecnico di Milano", "Politehnika u Milanu", "Italy", "Milano", "I  MILANO02");
            var upm = Partner("Universidad Politécnica de Madrid", "Politehničko sveučilište u Madridu", "Spain", "Madrid", "E  MADRID05");
            var empty = Partner("Aalto University", null, "Finland", "Espoo", "SF ESPOO12");
            var deleted = Partner("Closed Partner University", null, "Austria", "Wien", "A  WIEN99");
            deleted.IsDeleted = true;
            deleted.DeletedAt = DateTime.UtcNow.AddMonths(-2);
            db.Institutions.AddRange(tum, polimi, upm, empty, deleted);

            // Volume for paging, search and the country filter.
            string[] countries = ["Germany", "Italy", "Spain", "France", "Netherlands", "Portugal", "Sweden", "Poland", "Czechia", "Slovenia"];
            for (var i = 1; i <= 55; i++)
            {
                var country = countries[i % countries.Length];
                db.Institutions.Add(Partner($"Demo University {i:D2}", $"Demo sveučilište {i:D2}", country, $"City {i:D2}", $"DEMO{i:D3}"));
            }
            await db.SaveChangesAsync();

            _partners["tum"] = tum.Id;
            _partners["polimi"] = polimi.Id;
            _partners["upm"] = upm.Id;

            var tumCourses = new[]
            {
                Course(tum, "IN2064", "Machine Learning", "Strojno učenje", 8, ExchangeSemester.Winter, 4, 2, 0),
                Course(tum, "IN2003", "Efficient Algorithms and Data Structures", "Učinkoviti algoritmi i strukture podataka", 8, ExchangeSemester.Winter, 4, 2, 0),
                Course(tum, "IN2121", "Natural Language Processing", "Obrada prirodnog jezika", 6, ExchangeSemester.Summer, 3, 1, 0),
                Course(tum, "IN2346", "Introduction to Deep Learning", "Uvod u duboko učenje", 6, ExchangeSemester.Both, 2, 2, 0),
                Course(tum, "IN2211", "Information Retrieval", null, 6, ExchangeSemester.Winter, 3, 1, 0),
                Course(tum, "IN2259", "Data Analysis and Visualization", "Analiza i vizualizacija podataka", 5, ExchangeSemester.Summer, 2, 2, 0),
                Course(tum, "IN2097", "Advanced Computer Networking", "Napredne računalne mreže", 5, ExchangeSemester.Winter, 3, 0, 2),
                Course(tum, "IN2107", "Seminar Course", "Seminar", 5, ExchangeSemester.Both, 0, 2, 0),
                Course(tum, "IN2031", "Advanced Topics in Software Engineering", null, 5, ExchangeSemester.Summer, 2, 2, 0),
                Course(tum, "IN2106", "Practical Course: Web Applications", "Praktikum: web aplikacije", 10, ExchangeSemester.Both, 0, 0, 6),
                Course(tum, "IN0013", "Basic Principles: Databases", null, 5, ExchangeSemester.Winter, 3, 2, 0, StudyProgramLevel.Undergraduate),
            };
            var retired = Course(tum, "IN9999", "Retired Course (soft-deleted)", null, 5, ExchangeSemester.Winter, 2, 2, 0);
            retired.IsDeleted = true;
            retired.DeletedAt = DateTime.UtcNow.AddMonths(-1);
            db.PartnerCourses.AddRange(tumCourses);
            db.PartnerCourses.Add(retired);

            db.PartnerCourses.AddRange(
                Course(polimi, "054307", "Software Engineering 2", "Programsko inženjerstvo 2", 6, ExchangeSemester.Winter, 4, 2, 0),
                Course(polimi, "095898", "Computer Security", "Računalna sigurnost", 5, ExchangeSemester.Summer, 3, 2, 0),
                Course(polimi, "088949", "Recommender Systems", null, 5, ExchangeSemester.Winter, 3, 1, 0));

            // Large catalogue for paging/sorting/filters, plus near-duplicate codes for the merge tool.
            for (var i = 1; i <= 150; i++)
            {
                var semester = (ExchangeSemester)(i % 3);
                var level = i % 7 == 0 ? StudyProgramLevel.Undergraduate : StudyProgramLevel.Graduate;
                db.PartnerCourses.Add(Course(upm, $"UPM{1000 + i}", $"Course {i:D3}", $"Kolegij {i:D3}", 3 + i % 4, semester, 2, 1, i % 2, level));
            }
            db.PartnerCourses.AddRange(
                Course(upm, "ML101", "Machine Learning Basics", null, 6, ExchangeSemester.Winter, 3, 2, 0),
                Course(upm, "ML-101", "Machine learning basics", null, 6, ExchangeSemester.Winter, 3, 2, 0),
                Course(upm, "DB200", "Databases II", null, 6, ExchangeSemester.Summer, 3, 2, 0),
                Course(upm, "DB 200", "Databases 2", null, 6, ExchangeSemester.Summer, 3, 2, 0),
                Course(upm, "DB-200", "Databases II (duplicate)", null, 6, ExchangeSemester.Summer, 3, 2, 0),
                Course(upm, "SEC300", "Network Security", null, 5, ExchangeSemester.Both, 2, 2, 0),
                Course(upm, "SEC-300", "Network security", null, 5, ExchangeSemester.Both, 2, 2, 0));
            await db.SaveChangesAsync();

            foreach (var c in tumCourses) _courses[c.Code] = c.Id;
            _courses["054307"] = await db.PartnerCourses.Where(c => c.Code == "054307").Select(c => c.Id).SingleAsync();
            _courses["095898"] = await db.PartnerCourses.Where(c => c.Code == "095898").Select(c => c.Id).SingleAsync();
        });
        log.LogInformation("Catalogue: 60 partner institutions, {Count}+ partner courses", 170);
    }

    private static Institution Partner(string name, string? nameHr, string country, string city, string erasmusCode) => new()
    {
        Name = name, NameHr = nameHr, Country = country, City = city, ErasmusCode = erasmusCode, Type = InstitutionType.Partner,
    };

    private static PartnerCourse Course(Institution institution, string code, string name, string? nameHr, decimal ects,
        ExchangeSemester semester, int lectures, int auditory, int lab, StudyProgramLevel level = StudyProgramLevel.Graduate) => new()
    {
        Institution = institution, Code = code, Name = name, NameHr = nameHr, Ects = ects, Semester = semester, Level = level,
        LecturesH = lectures, AuditoryH = auditory, LabH = lab, Url = $"https://example.org/courses/{Uri.EscapeDataString(code)}",
    };

    // ---------------------------------------------------------------- accounts

    private int _ana, _ivo;

    private async Task SeedAccountsAsync()
    {
        _adminId = await CreateUser("admin@loom.dev", "Ada Admin");
        await WithDb(async db =>
        {
            var admin = await db.Users.SingleAsync(u => u.Id == _adminId);
            admin.Role = UserRole.Admin;
            await db.SaveChangesAsync();
        });
        await Onboard(_adminId, jmbag: null);

        // Coordinators get their role through the whitelist, as in production.
        foreach (var email in new[] { "ana.coordinator@loom.dev", "ivo.coordinator@loom.dev", "new.coordinator@loom.dev" })
            await Call<CoordinatorWhitelistService, CoordinatorWhitelistEntryResponse>(s => s.AddAsync(email, default), actor: _adminId);
        _ana = await CreateUser("ana.coordinator@loom.dev", "Ana Anić");
        _ivo = await CreateUser("ivo.coordinator@loom.dev", "Ivo Ivić");
        await Onboard(_ana, null);
        await Onboard(_ivo, null);

        var pending = await CreateUser("req.pending@loom.dev", "Petar Pending");
        await Call<AccountService, AuthMeResponse>(s => s.CompleteOnboardingAsync(new CompleteOnboardingRequest(HomeInstitutionId, RequestCoordinatorRole: true), default), actor: pending);
        var rejected = await CreateUser("req.rejected@loom.dev", "Rita Rejected");
        await Call<AccountService, AuthMeResponse>(s => s.CompleteOnboardingAsync(new CompleteOnboardingRequest(HomeInstitutionId, RequestCoordinatorRole: true), default), actor: rejected);
        await Call<CoordinatorRequestService, Success>(s => s.DecideAsync(rejected, "Rejected", default), actor: _adminId);

        await CreateUser("fresh.student@loom.dev", "Filip Fresh");   // not onboarded
        await CreateUser("claim.student@loom.dev", "Klara Claim");   // not onboarded, claims a placeholder through its access link
        log.LogInformation("Accounts: admin, 2 coordinators, whitelisted email, coordinator requests, onboarding users");
    }

    // ---------------------------------------------------------------- exchanges

    private async Task SeedExchangesAsync()
    {
        var tum = _partners["tum"];
        var c = _courses;

        // 1. Draft, empty
        var s1 = await Student("s.draft.empty@loom.dev", "Ema Empty", "0036100001");
        await NewExchange(1, s1, tum, coordinator: _ana);

        // 2. Draft, rich: all modes, a course split across slots, an over-filled slot
        var s2 = await Student("s.draft.rich@loom.dev", "Rene Rich", "0036100002");
        var g2 = await NewExchange(2, s2, tum, coordinator: _ana);
        await SaveLa(g2, s2,
            AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2003"], 5), AtExchange(SlotElective3, c["IN2064"], 3),
            AtExchange(SlotFree1, c["IN2097"], 5), AtExchange(SlotFree1, c["IN2259"], 5),
            Mode(SlotFree2, SlotMode.AfterExchange), Mode(SlotSeminar, SlotMode.AtHome), AtExchange(SlotTransversal, c["IN2107"], 2));

        // 3. Draft with a coordinator message
        var s3 = await Student("s.draft.message@loom.dev", "Marko Message", "0036100003");
        var g3 = await NewExchange(3, s3, tum, coordinator: _ana);
        await SaveLa(g3, s3, AtExchange(SlotElective1, c["IN2121"], 5), AtExchange(SlotElective2, c["IN2346"], 5));
        await Call<ExchangeService, ExchangeResponse>(s => s.UpdateCoordinatorMessageAsync(g3,
            "IN2121 is only offered in summer, please pick a winter course for this slot.", default), actor: _ana);

        // 4. Approved, ready for "Start final recognition"
        var s4 = await Student("s.approved@loom.dev", "Ana Approved", "0036100004");
        var g4 = await NewExchange(4, s4, tum, coordinator: _ana);
        await SaveLa(g4, s4, AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2003"], 5), AtExchange(SlotFree1, c["IN2211"], 5));
        await SetLaStatus(g4, DocumentStatus.Approved);

        // 5. Approved, then reopened by the coordinator for an amendment (draft again)
        var s5 = await Student("s.reopened@loom.dev", "Roko Reopened", "0036100005");
        var g5 = await NewExchange(5, s5, tum, coordinator: _ana);
        await SaveLa(g5, s5, AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2003"], 5));
        await SetLaStatus(g5, DocumentStatus.Approved);
        await SetLaStatus(g5, DocumentStatus.Draft);
        await SaveLa(g5, s5, AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2346"], 5));

        // 6. History: three approved versions, removed courses, one backup from a restore
        var s6 = await Student("s.history@loom.dev", "Hana History", "0036100006");
        var g6 = await NewExchange(6, s6, tum, coordinator: _ana);
        await SaveLa(g6, s6, AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2003"], 5), AtExchange(SlotElective3, c["IN2121"], 5));
        await SetLaStatus(g6, DocumentStatus.Approved);                                                   // v1
        await SetLaStatus(g6, DocumentStatus.Draft);
        await SaveLa(g6, s6, AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2003"], 5),
            AtExchange(SlotElective3, c["IN2346"], 5), AtExchange(SlotFree1, c["IN2211"], 5));
        await SetLaStatus(g6, DocumentStatus.Approved);                                                   // v2
        await SetLaStatus(g6, DocumentStatus.Draft);
        var firstApproval = (await Call<LaVersionService, List<DocumentVersionResponse>>(s => s.ListAsync(g6, default), actor: s6))
            .Single(x => x.VersionNo == 1);
        await Call<LaVersionService, RestoreResult>(s => s.RestoreAsync(g6, firstApproval.Id, default), actor: s6);   // creates a backup
        await SaveLa(g6, s6, AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2259"], 5),
            AtExchange(SlotElective3, c["IN2346"], 5), AtExchange(SlotFree1, c["IN2211"], 5));
        await SetLaStatus(g6, DocumentStatus.Approved);                                                   // v3

        // 7. Recognition in progress: grades partly entered, mapping scheme rearranged, one course not passed
        var s7 = await Student("s.recognition.draft@loom.dev", "Rea Recognition", "0036100007");
        var g7 = await NewExchange(7, s7, tum, coordinator: _ana);
        await RecognitionScenario(g7, s7, finish: false);

        // 8. Recognition approved by the coordinator (everything locked, official export)
        var s8 = await Student("s.recognition.done@loom.dev", "Dora Done", "0036100008");
        var g8 = await NewExchange(8, s8, tum, coordinator: _ana);
        await RecognitionScenario(g8, s8, finish: true);

        // 9-11. One student, three exchanges (exchange switcher, redirect view)
        var s9 = await Student("s.multi@loom.dev", "Mia Multi", "0036100009");
        await NewExchange(9, s9, tum, coordinator: _ana);
        await NewExchange(10, s9, _partners["polimi"], coordinator: _ana, semester: ExchangeSemester.Summer, studySemesters: [2]);
        await NewExchange(11, s9, _partners["upm"], coordinator: _ana, year: "2026/2027");

        // 12. No coordinator assigned
        var s12 = await Student("s.nocoordinator@loom.dev", "Nina Nocoord", "0036100012");
        await NewExchange(12, s12, tum, coordinator: null);

        // 13. Placeholder student created by the coordinator; opened through the access link (guest)
        var guest = await Placeholder("Gita Guest", "0036999001");
        var g13 = await NewExchange(13, guest, tum, coordinator: _ana, requester: _ana);
        await SaveLa(g13, guest, AtExchange(SlotElective1, c["IN2064"], 5));
        await AccessLink(13);

        // 14. Placeholder that claim.student@loom.dev takes over by opening its access link while signed in
        var claim = await Placeholder("Klara Claim", "0036999002");
        await NewExchange(14, claim, _partners["polimi"], coordinator: _ana, requester: _ana);
        await AccessLink(14);

        log.LogInformation("Exchanges: 14 scenario exchanges");
    }

    private async Task RecognitionScenario(Guid exchange, int student, bool finish)
    {
        var c = _courses;
        await SaveLa(exchange, student,
            AtExchange(SlotElective1, c["IN2064"], 5), AtExchange(SlotElective2, c["IN2003"], 5),
            AtExchange(SlotElective3, c["IN2064"], 3), AtExchange(SlotFree1, c["IN2121"], 5));
        await SetLaStatus(exchange, DocumentStatus.Approved);

        // "Start final recognition": freezes the LA and table 1, creates the results from the approved version.
        await Call<RecognitionService, RecognitionResponse>(s => s.StartAsync(exchange, default), actor: student);

        CourseGradesRequest Grade(string code, string status, string original, string ects, string hr) =>
            new(c[code], status, original, ects, hr, new DateOnly(2026, 2, 10));

        var grades = new List<CourseGradesRequest> { Grade("IN2064", "Passed", "1.3", "A", "5") };
        if (finish)
        {
            grades.Add(Grade("IN2003", "Passed", "2.0", "B", "4"));
            grades.Add(Grade("IN2121", "NotPassed", "5.0", "F", "1"));
        }
        await Call<RecognitionService, RecognitionResponse>(s => s.SaveGradesAsync(exchange, new SaveGradesRequest(grades), default), actor: student);

        // Rearrange the mapping scheme: move NLP to another slot and mark it not passed, split algorithms over two slots.
        var scheme = await Call<MappingSchemeService, MappingSchemeResponse>(s => s.GetAsync(exchange, default), actor: student);
        var entries = scheme.Entries.Select(e =>
        {
            var slot = e.HomeSlotId;
            var ects = e.AwardedEcts;
            var status = e.EnrollmentStatus?.ToString();
            if (e.PartnerCourseCode == "IN2121") { slot = SlotFree2; status = "NotPassed"; }
            if (e.PartnerCourseCode == "IN2003") ects = 3;
            return new SaveMappingSchemeEntryRequest(e.Id, slot, e.PartnerCourseId, ects, status, e.OriginalGrade, e.EctsGrade, e.HrGrade, e.ExamDate);
        }).ToList();
        var algorithms = scheme.Entries.First(e => e.PartnerCourseCode == "IN2003");
        entries.Add(new SaveMappingSchemeEntryRequest(0, SlotSeminar, algorithms.PartnerCourseId, 2,
            algorithms.EnrollmentStatus?.ToString(), algorithms.OriginalGrade, algorithms.EctsGrade, algorithms.HrGrade, algorithms.ExamDate));
        await Call<MappingSchemeService, MappingSchemeResponse>(s => s.SaveAsync(exchange, new SaveMappingSchemeRequest(entries), default), actor: student);

        if (finish)
            await Call<RecognitionService, RecognitionResponse>(s => s.SetStatusAsync(exchange, new UpdateRecognitionStatusRequest("Approved"), default), actor: _ana);
    }

    // ---------------------------------------------------------------- volume

    private async Task SeedVolumeAsync()
    {
        int[] partners = [_partners["tum"], _partners["polimi"], _partners["upm"]];
        string[] years = ["2024/2025", "2025/2026", "2026/2027"];
        for (var i = 1; i <= 40; i++)
        {
            var id = await Student($"student{i:D2}@loom.dev", $"Student {i:D2}", $"00362000{i:D2}");
            if (i <= 20)
                await NewExchange(100 + i, id, partners[i % 3], coordinator: _ana, year: years[i % 3]);
        }
        log.LogInformation("Volume: 40 extra students, 20 with exchanges");
    }

    // ---------------------------------------------------------------- helpers

    private async Task<int> CreateUser(string email, string name)
    {
        var user = await Call<UserSyncService, SyncedUser>(s => s.SyncAsync($"dev:{email}", email, name, default));
        return user.Id;
    }

    private Task Onboard(int userId, string? jmbag) =>
        Call<AccountService, AuthMeResponse>(s => s.CompleteOnboardingAsync(new CompleteOnboardingRequest(HomeInstitutionId, jmbag), default), actor: userId);

    private async Task<int> Student(string email, string name, string jmbag)
    {
        var id = await CreateUser(email, name);
        await Onboard(id, jmbag);
        return id;
    }

    private Task AccessLink(int n) => WithDb(async db =>
    {
        var exchangeId = await db.Exchanges.Where(e => e.Guid == ExchangeGuid(n)).Select(e => e.Id).SingleAsync();
        db.ExchangeAccessLinks.Add(new ExchangeAccessLink { ExchangeId = exchangeId, Token = AccessToken(n), CreatedById = _ana });
        await db.SaveChangesAsync();
    });

    private async Task<int> Placeholder(string name, string jmbag)
    {
        var created = await Call<StudentService, CoordinatorStudentResponse>(s =>
            s.CreatePlaceholderAsync(new PlaceholderStudentRequest(name, jmbag, HomeInstitutionId), default), actor: _ana);
        return created.Id;
    }

    private async Task<Guid> NewExchange(int number, int student, int partnerInstitution, int? coordinator,
        int? requester = null, string year = Year, ExchangeSemester semester = ExchangeSemester.Winter, List<int>? studySemesters = null)
    {
        var request = new CreateExchangeRequest(ProfileId, partnerInstitution, year, semester.ToString(), studySemesters ?? [3],
            CoordinatorId: coordinator, TargetStudentId: requester is null ? null : student);
        var created = await Call<ExchangeService, ExchangeResponse>(s => s.CreateAsync(request, default), actor: requester ?? student);

        var guid = ExchangeGuid(number);
        await WithDb(async db =>
        {
            var exchange = await db.Exchanges.SingleAsync(e => e.Id == created.Id);
            exchange.Guid = guid;
            await db.SaveChangesAsync();
        });
        return guid;
    }

    private static LearningAgreementEntryUpsertDto AtExchange(int slot, int course, decimal ects) =>
        new(slot, nameof(SlotMode.AtExchange), course, ects);

    private static LearningAgreementEntryUpsertDto Mode(int slot, SlotMode mode) => new(slot, mode.ToString(), null, null);

    private Task SaveLa(Guid exchange, int requester, params LearningAgreementEntryUpsertDto[] entries) =>
        Call<LearningAgreementService, LearningAgreementResponse>(s =>
            s.SaveAsync(exchange, new SaveLearningAgreementRequest([.. entries]), default), actor: requester);

    private Task SetLaStatus(Guid exchange, DocumentStatus status) =>
        Call<LearningAgreementWorkflow, ExchangeResponse>(s =>
            s.SetStatusAsync(exchange, new UpdateLearningAgreementStatusRequest(status.ToString()), default), actor: _ana);

    /// <summary>One DI scope per call, like one HTTP request. Any error stops the seed.</summary>
    private async Task<T> Call<TService, T>(Func<TService, Task<ErrorOr<T>>> call, int? actor = null) where TService : notnull
    {
        await using var scope = services.CreateAsyncScope();
        if (actor is not null) scope.ServiceProvider.GetRequiredService<CurrentActor>().Set(actor.Value);
        var result = await call(scope.ServiceProvider.GetRequiredService<TService>());
        if (result.IsError)
            throw new InvalidOperationException($"Seed step failed: {result.FirstError.Code} - {result.FirstError.Description}");
        return result.Value;
    }

    private async Task WithDb(Func<AppDbContext, Task> action)
    {
        await using var scope = services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
