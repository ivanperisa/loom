using Loom.Application.Common.Security;
using Loom.Application.Features.Admin;
using Loom.Application.Features.Catalog;
using Loom.Application.Features.Completion;
using Loom.Application.Features.Coordination;
using Loom.Application.Features.Documents;
using Loom.Application.Features.Exchanges;
using Loom.Application.Features.Planning;
using Loom.Application.Features.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddScoped<CurrentActor>();
        services.AddScoped<ICurrentActor>(sp => sp.GetRequiredService<CurrentActor>());
        services.AddScoped<ExchangeAccess>();

        // Catalog
        services.AddScoped<HomeCatalogService>();
        services.AddScoped<PartnerInstitutionService>();
        services.AddScoped<PartnerCourseService>();
        services.AddScoped<CourseMergeService>();

        // Users, admin, coordination
        services.AddScoped<UserSyncService>();
        services.AddScoped<AccountService>();
        services.AddScoped<PlaceholderClaimService>();
        services.AddScoped<AdminUserService>();
        services.AddScoped<CoordinatorRequestService>();
        services.AddScoped<CoordinatorWhitelistService>();
        services.AddScoped<CoordinatorDirectoryService>();
        services.AddScoped<StudentService>();

        // Exchanges
        services.AddScoped<ExchangeService>();
        services.AddScoped<AccessLinkService>();

        // Documents (versions shared by the LA and the recognition)
        services.AddScoped<VersionStore>();

        // Planning (learning agreement)
        services.AddScoped<LaEntryWriter>();
        services.AddScoped<LaContent>();
        services.AddScoped<LearningAgreementService>();
        services.AddScoped<LearningAgreementWorkflow>();
        services.AddScoped<LaVersionService>();
        services.AddScoped<LaTransferService>();

        // Completion (recognition, mapping scheme)
        services.AddScoped<ResultsGuard>();
        services.AddScoped<RecognitionService>();
        services.AddScoped<MappingSchemeService>();

        return services;
    }
}
