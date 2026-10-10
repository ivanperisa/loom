using System.Net;
using System.Net.Http.Json;
using Loom.Application.Features.Users;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Loom.IntegrationTests;

/// <summary>Access links: guest sessions, their scope, revocation and claiming the placeholder.</summary>
public class AccessLinkTests(DatabaseFixture fixture) : HttpTest(fixture)
{
    private sealed record Setup(int Partner, int Coordinator, string CoordinatorEmail, int Placeholder, string PlaceholderJmbag, Guid Exchange, Guid OtherExchange);

    private record TokenBody(string Token);
    private record SessionBody(Guid ExchangeGuid);
    private record PreviewBody(Guid ExchangeGuid, bool HasAccess, bool CanClaim);
    private record CourseBody(int Id);

    /// <summary>A coordinator with a placeholder student who has two exchanges.</summary>
    private async Task<Setup> Arrange()
    {
        var (partner, _) = await NewPartner();
        var coordinator = await NewUser(UserRole.Coordinator);
        var coordinatorEmail = await Email(coordinator);
        var jmbag = Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
        var placeholder = await Db(async db =>
        {
            var user = new User { ExternalId = jmbag, Email = "", Name = "Placeholder", Role = UserRole.Student, IsOnboarded = true, Jmbag = jmbag, InstitutionId = 1, CoordinatorId = coordinator };
            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);
            return user.Id;
        });
        var exchange = await NewExchange(placeholder, partner, coordinator);
        var other = await NewExchange(placeholder, partner, coordinator);
        return new Setup(partner, coordinator, coordinatorEmail, placeholder, jmbag, exchange, other);
    }

    private Task<string> Email(int userId) => Db(db => db.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync(Ct));

    private static async Task<string> LinkToken(HttpClient coordinator, Guid exchange, bool regenerate = false)
    {
        var response = await coordinator.PostAsync($"/api/exchanges/{exchange}/access-link{(regenerate ? "/regenerate" : "")}", null, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenBody>(Ct))!.Token;
    }

    private static async Task<HttpClient> Guest(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/access/session", new { token }, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    [Fact]
    public async Task The_coordinator_gets_the_same_link_until_it_is_regenerated()
    {
        await using var factory = Factory();
        var s = await Arrange();
        var coordinator = await LoggedIn(factory, s.CoordinatorEmail);

        var first = await LinkToken(coordinator, s.Exchange);
        Assert.Equal(first, await LinkToken(coordinator, s.Exchange));
        Assert.Equal(43, first.Length);
        Assert.NotEqual(s.Exchange.ToString(), first);

        var rotated = await LinkToken(coordinator, s.Exchange, regenerate: true);
        Assert.NotEqual(first, rotated);
        Assert.Equal(rotated, await LinkToken(coordinator, s.Exchange));

        // The student cannot manage links.
        var student = await LoggedIn(factory, await Email(await NewUser()));
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsync($"/api/exchanges/{s.Exchange}/access-link", null, Ct)).StatusCode);
    }

    [Fact]
    public async Task A_guest_works_on_exactly_one_exchange()
    {
        await using var factory = Factory();
        var s = await Arrange();
        var coordinator = await LoggedIn(factory, s.CoordinatorEmail);
        var guest = await Guest(factory, await LinkToken(coordinator, s.Exchange));

        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync($"/api/exchanges/{s.Exchange}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync($"/api/exchanges/{s.Exchange}/learning-agreement", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync($"/api/exchanges/{s.Exchange}/partner-courses", Ct)).StatusCode);

        // Same placeholder student, different exchange: the link does not open it.
        var other = await guest.GetAsync($"/api/exchanges/{s.OtherExchange}", Ct);
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);

        // Not for guests: deleting, link management, anything outside the exchange.
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.DeleteAsync($"/api/exchanges/{s.Exchange}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync($"/api/exchanges/{s.Exchange}/access-link/regenerate", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("/api/exchanges/mine", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/api/partner-institutions/{s.Partner}/courses", Ct)).StatusCode);
    }

    [Fact]
    public async Task Guests_add_courses_only_to_their_exchange_partner()
    {
        await using var factory = Factory();
        var s = await Arrange();
        var coordinator = await LoggedIn(factory, s.CoordinatorEmail);
        var guest = await Guest(factory, await LinkToken(coordinator, s.Exchange));
        var course = new { code = $"G{Guid.NewGuid():N}"[..10], name = "Guest course", ects = 5, semester = "Winter", level = "Graduate" };

        var created = await guest.PostAsJsonAsync($"/api/exchanges/{s.Exchange}/partner-courses", course, Ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var courseId = (await created.Content.ReadFromJsonAsync<CourseBody>(Ct))!.Id;
        Assert.Equal(s.Partner, await Db(db => db.PartnerCourses.Where(c => c.Id == courseId).Select(c => c.InstitutionId).SingleAsync(Ct)));

        // The catalogue endpoint (any institution) is admin-only now; it used to be open to everyone.
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().PostAsJsonAsync($"/api/partner-institutions/{s.Partner}/courses", course, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinator.PostAsJsonAsync($"/api/partner-institutions/{s.Partner}/courses", course, Ct)).StatusCode);
    }

    [Fact]
    public async Task Regenerating_the_link_ends_open_guest_sessions()
    {
        await using var factory = Factory();
        var s = await Arrange();
        var coordinator = await LoggedIn(factory, s.CoordinatorEmail);
        var oldToken = await LinkToken(coordinator, s.Exchange);
        var guest = await Guest(factory, oldToken);

        await LinkToken(coordinator, s.Exchange, regenerate: true);

        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/api/exchanges/{s.Exchange}", Ct)).StatusCode);
        var reopened = await factory.CreateClient().PostAsJsonAsync("/api/access/session", new { token = oldToken }, Ct);
        Assert.Equal(HttpStatusCode.NotFound, reopened.StatusCode);
        Assert.Equal("ACCESS_LINK_INVALID", await ErrorCode(reopened));
    }

    [Fact]
    public async Task A_signed_in_student_claims_the_placeholder_with_the_link()
    {
        await using var factory = Factory();
        var s = await Arrange();
        var coordinator = await LoggedIn(factory, s.CoordinatorEmail);
        var token = await LinkToken(coordinator, s.Exchange);
        var guest = await Guest(factory, token);

        var studentId = await NewUser();
        var student = await LoggedIn(factory, await Email(studentId));
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/exchanges/{s.Exchange}", Ct)).StatusCode);

        var preview = await (await student.PostAsJsonAsync("/api/access/preview", new { token }, Ct)).Content.ReadFromJsonAsync<PreviewBody>(Ct);
        Assert.Equal(new PreviewBody(s.Exchange, HasAccess: false, CanClaim: true), preview);

        var claimed = await student.PostAsJsonAsync("/api/access/claim", new { token }, Ct);
        Assert.Equal(HttpStatusCode.OK, claimed.StatusCode);
        Assert.Equal(s.Exchange, (await claimed.Content.ReadFromJsonAsync<SessionBody>(Ct))!.ExchangeGuid);

        // Both of the placeholder's exchanges are the student's now; the placeholder and its link are gone.
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/exchanges/{s.Exchange}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/exchanges/{s.OtherExchange}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/api/exchanges/{s.Exchange}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.PostAsJsonAsync("/api/access/claim", new { token }, Ct)).StatusCode);

        var user = await Db(db => db.Users.AsNoTracking().SingleAsync(u => u.Id == studentId, Ct));
        Assert.Equal(s.PlaceholderJmbag, user.Jmbag);
        Assert.Equal(s.Coordinator, user.CoordinatorId);
        Assert.False(await Db(db => db.Users.AnyAsync(u => u.Id == s.Placeholder, Ct)));
    }

    [Fact]
    public async Task The_coordinator_opening_the_link_goes_to_the_exchange_instead_of_claiming()
    {
        await using var factory = Factory();
        var s = await Arrange();
        var coordinator = await LoggedIn(factory, s.CoordinatorEmail);
        var token = await LinkToken(coordinator, s.Exchange);

        var preview = await (await coordinator.PostAsJsonAsync("/api/access/preview", new { token }, Ct)).Content.ReadFromJsonAsync<PreviewBody>(Ct);
        Assert.Equal(new PreviewBody(s.Exchange, HasAccess: true, CanClaim: false), preview);

        var claim = await coordinator.PostAsJsonAsync("/api/access/claim", new { token }, Ct);
        Assert.Equal(HttpStatusCode.Forbidden, claim.StatusCode);
        Assert.Equal("CLAIM_NOT_ALLOWED", await ErrorCode(claim));
    }

    [Fact]
    public async Task Typing_a_placeholder_JMBAG_during_onboarding_gives_no_access()
    {
        var s = await Arrange();
        var newcomer = await Db(async db =>
        {
            var user = new User { ExternalId = $"test:{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@test.local", Name = "New", Role = UserRole.Student };
            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);
            return user.Id;
        });

        var result = await Call<AccountService, AuthMeResponse>(a => a.CompleteOnboardingAsync(new CompleteOnboardingRequest(1, s.PlaceholderJmbag), Ct), actor: newcomer);

        Assert.True(result.IsError);
        Assert.Equal("JMBAG_RESERVED", result.FirstError.Code);
        Assert.True(await Db(db => db.Exchanges.AllAsync(e => e.StudentId != newcomer, Ct)));
    }
}
