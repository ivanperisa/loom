using Loom.Application.Common.Security;
using Loom.Application.Features.Admin;
using Loom.Application.Features.Catalog;
using Loom.Application.Features.Coordination;
using Loom.Application.Features.Users;
using Loom.Application.Helpers;
using Loom.Application.Interfaces.Services;
using Loom.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<CachedQuery>();

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
        services.AddScoped<AdminUserService>();
        services.AddScoped<CoordinatorRequestService>();
        services.AddScoped<CoordinatorWhitelistService>();
        services.AddScoped<CoordinatorDirectoryService>();
        services.AddScoped<StudentService>();

        services.AddScoped<IExchangeService, ExchangeService>();
        services.AddScoped<ILearningAgreementService, LearningAgreementService>();
        services.AddScoped<IRecognitionService, RecognitionService>();
        services.AddScoped<IMappingSchemeService, MappingSchemeService>();

        return services;
    }
}
