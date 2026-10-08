using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Helpers;
using Loom.Application.Interfaces.Services;
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
    public async Task Draft_exchange_without_recognition_can_be_deleted()
    {
        var student = await NewUser();
        var (partner, _) = await NewPartner();
        var exchange = await NewExchange(student, partner, coordinator: null);

        await Ok<IExchangeService, ErrorOr.Deleted>(s => s.DeleteExchangeAsync(exchange, student, Ct));

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

        await Ok<IInstitutionService, PartnerCourseResponse>(s =>
            s.MergePartnerCoursesAsync(new Application.DTOs.Institution.MergePartnerCoursesRequest(courses["A"], [courses["C"]]), Ct));

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

        var result = await Call<IInstitutionService, PartnerCourseResponse>(s =>
            s.MergePartnerCoursesAsync(new Application.DTOs.Institution.MergePartnerCoursesRequest(courses["A"], [courses["D"]]), Ct));

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

        await Ok<IAdminService, UserListResponse>(s => s.SetUserRoleAsync(admin, target, UserRole.Coordinator, Ct));

        Assert.False(cache.TryGetValue(UserSyncCache.Key(externalId), out _));
    }
}
