using Loom.Application.Common.Security;
using Loom.Application.Features.Exchanges;
using Loom.Application.Features.Catalog;
using Loom.Application.Features.Admin;
using Loom.Application.Features.Planning;
using Loom.Application.Features.Coordination;
using Loom.Application.Common.Querying;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.IntegrationTests;

public class CatalogAndAccountTests(DatabaseFixture fixture) : IntegrationTest(fixture)
{
    private readonly DatabaseFixture _fixture = fixture;

    [Fact]
    public async Task A_coordinators_student_list_carries_their_exchanges_and_filters()
    {
        var coordinator = await NewUser(UserRole.Coordinator);
        var student = await NewUser();
        var (partnerA, _) = await NewPartner();
        var (partnerB, _) = await NewPartner();
        var first = await NewExchange(student, partnerA, coordinator);
        var second = await NewExchange(student, partnerB, coordinator);
        var partnerBName = await Db(db => db.Institutions.Where(p => p.Id == partnerB).Select(p => p.Name).SingleAsync(Ct));

        var all = await Ok<StudentService, PagedResponse<CoordinatorStudentResponse>>(s => s.ListMineAsync(new StudentListQuery(), Ct), actor: coordinator);
        var row = Assert.Single(all.Items);
        Assert.Equal([second, first], row.Exchanges.Select(e => e.Guid));   // newest first

        var filtered = await Ok<StudentService, PagedResponse<CoordinatorStudentResponse>>(
            s => s.ListMineAsync(new StudentListQuery { PartnerInstitution = partnerBName }, Ct), actor: coordinator);
        Assert.Equal([second], Assert.Single(filtered.Items).Exchanges.Select(e => e.Guid));

        var filters = await Ok<StudentService, StudentFiltersResponse>(s => s.FiltersAsync(Ct), actor: coordinator);
        Assert.Equal(["2025/2026"], filters.AcademicYears);
        Assert.Equal(2, filters.PartnerInstitutions.Count);
    }

    [Fact]
    public async Task A_coordinator_request_is_approved_or_rejected_once()
    {
        var admin = await NewUser(UserRole.Admin);
        var approved = await NewUser();
        var rejected = await NewUser();
        await Db(async db =>
        {
            foreach (var user in await db.Users.Where(u => u.Id == approved || u.Id == rejected).ToListAsync(Ct))
                user.CoordinatorRequestStatus = CoordinatorRequestStatus.Pending;
            return await db.SaveChangesAsync(Ct);
        });

        Assert.Equal("INVALID_STATUS", (await Call<CoordinatorRequestService, ErrorOr.Success>(s => s.DecideAsync(approved, "Maybe", Ct), admin)).FirstError.Code);
        await Ok<CoordinatorRequestService, ErrorOr.Success>(s => s.DecideAsync(approved, "Approved", Ct), admin);
        await Ok<CoordinatorRequestService, ErrorOr.Success>(s => s.DecideAsync(rejected, "Rejected", Ct), admin);

        var users = await Db(db => db.Users.Where(u => u.Id == approved || u.Id == rejected).ToDictionaryAsync(u => u.Id, Ct));
        Assert.Equal((UserRole.Coordinator, null), (users[approved].Role, users[approved].CoordinatorRequestStatus));
        Assert.Equal((UserRole.Student, CoordinatorRequestStatus.Rejected), (users[rejected].Role, users[rejected].CoordinatorRequestStatus));
        Assert.Equal("NO_PENDING_REQUEST", (await Call<CoordinatorRequestService, ErrorOr.Success>(s => s.DecideAsync(rejected, "Approved", Ct), admin)).FirstError.Code);
    }

    [Fact]
    public async Task Draft_exchange_without_recognition_can_be_deleted()
    {
        var student = await NewUser();
        var (partner, _) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator: null);

        await Ok<ExchangeService, ErrorOr.Deleted>(s => s.DeleteAsync(exchange, Ct), actor: student);

        Assert.False(await Db(db => db.Exchanges.AnyAsync(e => e.Guid == exchange, Ct)));
    }

    [Fact]
    public async Task Merging_courses_moves_learning_agreement_and_mapping_scheme_links()
    {
        var student = await NewUser();
        var (partner, courses) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator: null);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 5), AtExchange(Slot2, courses["C"], 5));
        await Db(async db =>
        {
            var exchangeId = await db.Exchanges.Where(e => e.Guid == exchange).Select(e => e.Id).SingleAsync(Ct);
            db.MappingSchemeEntries.Add(new MappingSchemeEntry { ExchangeId = exchangeId, HomeSlotId = Slot3, PartnerCourseId = courses["C"], AwardedEcts = 2 });
            return await db.SaveChangesAsync(Ct);
        });

        await Ok<CourseMergeService, PartnerCourseResponse>(s => s.MergeAsync(new MergePartnerCoursesRequest(courses["A"], [courses["C"]]), Ct));

        Assert.True(await Db(db => db.LearningAgreementEntries.AnyAsync(e => e.HomeSlotId == Slot2 && e.PartnerCourseId == courses["A"], Ct)));
        Assert.True(await Db(db => db.MappingSchemeEntries.AnyAsync(e => e.HomeSlotId == Slot3 && e.PartnerCourseId == courses["A"], Ct)));
        Assert.False(await Db(db => db.PartnerCourses.AnyAsync(c => c.Id == courses["C"], Ct)));
    }

    [Fact]
    public async Task Merge_refuses_when_two_merged_courses_share_a_slot()
    {
        var student = await NewUser();
        var (partner, courses) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator: null);
        await SaveLa(exchange, student, AtExchange(Slot1, courses["A"], 3), AtExchange(Slot1, courses["D"], 2));

        var result = await Call<CourseMergeService, PartnerCourseResponse>(s => s.MergeAsync(new MergePartnerCoursesRequest(courses["A"], [courses["D"]]), Ct));

        Assert.Equal("MERGE_CONFLICT", result.FirstError.Code);
    }

    [Fact]
    public async Task Role_change_evicts_the_cached_role()
    {
        var admin = await NewUser(UserRole.Admin);
        var target = await NewUser();
        var externalId = await Db(db => db.Users.Where(u => u.Id == target).Select(u => u.ExternalId).SingleAsync(Ct));
        var cache = _fixture.Services.GetRequiredService<IMemoryCache>();
        cache.Set(UserSyncCache.Key(externalId), "stale");

        await Ok<AdminUserService, UserListResponse>(s => s.SetRoleAsync(target, UserRole.Coordinator, Ct), actor: admin);

        Assert.False(cache.TryGetValue(UserSyncCache.Key(externalId), out _));
    }
}
