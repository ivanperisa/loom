using ErrorOr;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Interfaces.Services;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Loom.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.IntegrationTests;

[Collection(nameof(DatabaseCollection))]
public abstract class IntegrationTest(DatabaseFixture fixture)
{
    protected const int ProfileId = 101;        // reference data: profile with slots 200-221
    protected const int Slot1 = 214, Slot2 = 215, Slot3 = 216;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Calls a service in its own DI scope (one "request").</summary>
    protected async Task<ErrorOr<T>> Call<TService, T>(Func<TService, Task<ErrorOr<T>>> call) where TService : notnull
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <summary>Like <see cref="Call{TService,T}"/> but fails the test on an error result.</summary>
    protected async Task<T> Ok<TService, T>(Func<TService, Task<ErrorOr<T>>> call) where TService : notnull
    {
        var result = await Call(call);
        Assert.False(result.IsError, result.IsError ? $"{result.FirstError.Code}: {result.FirstError.Description}" : null);
        return result.Value;
    }

    protected async Task<T> Db<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected Task<int> NewUser(UserRole role = UserRole.Student) => Db(async db =>
    {
        var key = Guid.NewGuid().ToString("N")[..12];
        var user = new User { ExternalId = $"test:{key}", Email = $"{key}@test.local", Name = $"User {key}", Role = role, IsOnboarded = true, InstitutionId = 1 };
        db.Users.Add(user);
        await db.SaveChangesAsync(Ct);
        return user.Id;
    });

    /// <summary>A partner institution with courses A, B, C, D (6 ECTS each). Returns the institution id and course ids by code.</summary>
    protected Task<(int InstitutionId, Dictionary<string, int> Courses)> NewPartner() => Db(async db =>
    {
        var institution = new Institution { Name = $"Partner {Guid.NewGuid():N}", Country = "DE", Type = InstitutionType.Partner };
        var courses = new[] { "A", "B", "C", "D" }.Select(code => new PartnerCourse
        {
            Institution = institution, Code = code, Name = code, Ects = 6,
            Semester = ExchangeSemester.Winter, Level = StudyProgramLevel.Graduate,
        }).ToList();
        db.PartnerCourses.AddRange(courses);
        await db.SaveChangesAsync(Ct);
        return (institution.Id, courses.ToDictionary(c => c.Code, c => c.Id));
    });

    protected async Task<Guid> NewExchange(int student, int partnerInstitution, int? coordinator)
    {
        var created = await Ok<IExchangeService, ExchangeResponse>(s => s.CreateExchangeAsync(student,
            new CreateExchangeRequest(ProfileId, partnerInstitution, "2025/2026", "Winter", [3], CoordinatorId: coordinator), Ct));
        return created.Guid;
    }

    protected Task SaveLa(Guid exchange, int requester, params LearningAgreementEntryUpsertDto[] entries) =>
        Ok<ILearningAgreementService, LearningAgreementResponse>(s =>
            s.SaveLearningAgreementAsync(exchange, requester, new SaveLearningAgreementRequest([.. entries]), Ct));

    protected Task<ErrorOr<ExchangeResponse>> SetLaStatus(Guid exchange, int requester, string status) =>
        Call<ILearningAgreementService, ExchangeResponse>(s =>
            s.UpdateLearningAgreementStatusAsync(exchange, requester, new UpdateLearningAgreementStatusRequest(status), Ct));

    protected static LearningAgreementEntryUpsertDto AtExchange(int slot, int course, decimal ects) => new(slot, "AtExchange", course, ects);
}
