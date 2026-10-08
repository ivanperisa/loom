using System.Text.Json;
using Loom.Application.Common;
using Loom.Application.Features.Planning;
using Loom.DevSeed;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Loom.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.IntegrationTests;

public class MigrationTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Model_has_no_changes_without_a_migration()
    {
        var pending = await Db(db => Task.FromResult(db.Database.HasPendingModelChanges()));
        Assert.False(pending, "The EF model changed but no migration was added (dotnet ef migrations add <Name>).");
    }

    [Fact]
    public async Task Demo_seed_runs_end_to_end_on_a_fresh_database()
    {
        var connectionString = _fixture.ConnectionStringFor("seed_" + Guid.NewGuid().ToString("N")[..8]);
        await using var services = DatabaseFixture.BuildServices(connectionString);

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(Ct);
            await ReferenceData.LoadAsync(db);
        }

        // Every scenario goes through the real services; any failing step throws.
        await new DemoData(services, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).SeedAsync();

        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scenarioGuids = Enumerable.Range(1, 14).Select(DemoData.ExchangeGuid).ToList();
            Assert.Equal(14, await db.Exchanges.CountAsync(e => scenarioGuids.Contains(e.Guid), Ct));
            Assert.True(await db.Recognitions.AnyAsync(r => r.Status == Domain.Enums.DocumentStatus.Approved, Ct));
        }
    }

    /// <summary>
    /// Builds the old document model (snapshots, recognition_entry, Submitted status) at the migration before
    /// DocumentVersions, migrates, and checks that history, amendment marks and grades came over.
    /// </summary>
    [Fact]
    public async Task Old_documents_are_carried_over_into_versions()
    {
        var connectionString = _fixture.ConnectionStringFor("docs_" + Guid.NewGuid().ToString("N")[..8]);
        await using var services = DatabaseFixture.BuildServices(connectionString);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.GetService<IMigrator>().MigrateAsync("20261008225749_AddAccessLinks", Ct);
        await ReferenceData.LoadAsync(db);

        // Tables whose shape did not change can be written with the current model.
        var student = new User { ExternalId = "old-student", Email = "old@test.local", Name = "Old", Role = UserRole.Student, IsOnboarded = true };
        var coordinator = new User { ExternalId = "old-coord", Email = "oldc@test.local", Name = "Coord", Role = UserRole.Coordinator, IsOnboarded = true };
        var partner = new Institution { Name = "Old Partner", Country = "DE", Type = InstitutionType.Partner };
        var courses = new[] { "A", "B", "C", "D" }.Select(code => new PartnerCourse
        {
            Institution = partner, Code = code, Name = code, Ects = 6, Semester = ExchangeSemester.Winter, Level = StudyProgramLevel.Graduate,
        }).ToList();
        db.AddRange(student, coordinator);
        db.PartnerCourses.AddRange(courses);
        await db.SaveChangesAsync(Ct);
        var (a, b, c, d) = (courses[0].Id, courses[1].Id, courses[2].Id, courses[3].Id);
        Exchange NewOldExchange() => new()
        {
            StudentId = student.Id, CoordinatorId = coordinator.Id, HomeProfileId = 101, PartnerInstitutionId = partner.Id,
            AcademicYear = "2024/2025", SemesterType = ExchangeSemester.Winter, StudySemesters = [3],
        };
        var planned = NewOldExchange();
        var graded = NewOldExchange();
        db.Exchanges.AddRange(planned, graded);
        await db.SaveChangesAsync(Ct);

        static string Snapshot(params (int Slot, int Course)[] rows) => JsonSerializer.Serialize(new
        {
            entries = rows.Select(r => new { homeSlotId = r.Slot, homeSlotLabel = "Slot", homeSlotSemester = 3, homeSlotEcts = 5, mode = "AtExchange", partnerCourseId = r.Course, partnerCourseCode = "X", partnerCourseName = "X", awardedEcts = 5.0m }),
        });
        // Planned: approved twice (v1: A, B; v2: A, C), then a draft that drops C and adds D. One pre-import backup.
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO exchange.learning_agreement (exchange_id, status) VALUES ({planned.Id}, 'Submitted'), ({graded.Id}, 'Approved');
            INSERT INTO exchange.learning_agreement_entry (learning_agreement_id, home_slot_id, mode, partner_course_id, awarded_ects)
            SELECT la.id, v.slot, 'AtExchange', v.course, 5 FROM exchange.learning_agreement la,
                   (VALUES (214, {a}, {planned.Id}), (216, {d}, {planned.Id}), (214, {a}, {graded.Id}), (215, {b}, {graded.Id})) v(slot, course, ex)
            WHERE la.exchange_id = v.ex;
            INSERT INTO exchange.snapshot (exchange_id, changed_by_id, phase, type, snapshot, created_at) VALUES
                ({planned.Id}, {coordinator.Id}, 'LearningAgreement', 'Auto', {Snapshot((214, a), (215, b))}::jsonb, now() - interval '3 days'),
                ({planned.Id}, {student.Id}, 'LearningAgreement', 'PreImport', {Snapshot((214, a))}::jsonb, now() - interval '2 days'),
                ({planned.Id}, {coordinator.Id}, 'LearningAgreement', 'Auto', {Snapshot((214, a), (215, c))}::jsonb, now() - interval '1 day'),
                ({graded.Id}, {coordinator.Id}, 'LearningAgreement', 'Auto', {Snapshot((214, a), (215, b))}::jsonb, now() - interval '1 day');
            INSERT INTO exchange.recognition (exchange_id, status) VALUES ({graded.Id}, 'Rejected');
            INSERT INTO exchange.recognition_entry (recognition_id, learning_agreement_entry_id, enrollment_status, original_grade, ects_grade)
            SELECT r.id, le.id, 'Passed', '1.3', 'A'
            FROM exchange.recognition r JOIN exchange.learning_agreement la ON la.exchange_id = r.exchange_id
            JOIN exchange.learning_agreement_entry le ON le.learning_agreement_id = la.id AND le.partner_course_id = {a}
            WHERE r.exchange_id = {graded.Id};
            """, Ct);

        await db.Database.MigrateAsync(Ct);
        db.ChangeTracker.Clear();

        var versions = await db.DocumentVersions.Where(v => v.ExchangeId == planned.Id).OrderBy(v => v.CreatedAt).ToListAsync(Ct);
        Assert.Equal([(VersionKind.Approved, (int?)1), (VersionKind.Backup, null), (VersionKind.Approved, 2)], versions.Select(v => (v.Kind, v.VersionNo)));
        // The SQL hash equals the application's, so approving unchanged content later does not add a version.
        var v2 = JsonSerializer.Deserialize<LaVersionPayload>(versions[2].Payload, JsonHelper.DefaultOptions)!;
        Assert.Equal(LaContent.Hash(v2), versions[2].ContentHash);

        var entries = await db.LearningAgreementEntries.Where(e => e.LearningAgreement.ExchangeId == planned.Id)
            .Select(e => new { Course = e.PartnerCourseId!.Value, e.IsDeleted, e.AddedInVersion, e.RemovedInVersion })
            .ToListAsync(Ct);
        Assert.Equivalent(new[]
        {
            new { Course = a, IsDeleted = false, AddedInVersion = (int?)1, RemovedInVersion = (int?)null },
            new { Course = d, IsDeleted = false, AddedInVersion = (int?)null, RemovedInVersion = (int?)null },   // new in the draft
            new { Course = b, IsDeleted = true, AddedInVersion = (int?)1, RemovedInVersion = (int?)2 },          // removed in A1
            new { Course = c, IsDeleted = true, AddedInVersion = (int?)2, RemovedInVersion = (int?)null },       // removal pending
        }, entries);
        Assert.Equal(DocumentStatus.Draft, await db.LearningAgreements.Where(l => l.ExchangeId == planned.Id).Select(l => l.Status).SingleAsync(Ct));

        // Graded: the grades moved into the results, which means final recognition had started.
        var results = await db.MappingSchemeEntries.Where(e => e.ExchangeId == graded.Id).ToListAsync(Ct);
        Assert.Equal(2, results.Count);
        Assert.Equal((EnrollmentStatus.Passed, "1.3", "A"), results.Where(e => e.PartnerCourseId == a).Select(e => (e.EnrollmentStatus!.Value, e.OriginalGrade, e.EctsGrade)).Single());
        Assert.NotNull(await db.LearningAgreements.Where(l => l.ExchangeId == graded.Id).Select(l => l.ConcludedAt).SingleAsync(Ct));
        Assert.Equal(DocumentStatus.Draft, await db.Recognitions.Where(r => r.ExchangeId == graded.Id).Select(r => r.Status).SingleAsync(Ct));
    }
}
