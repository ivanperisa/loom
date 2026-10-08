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

        services.AddScoped<UserService>();
        services.AddScoped<IUserService>(sp => sp.GetRequiredService<UserService>());
        services.AddScoped<IUserSyncService>(sp => sp.GetRequiredService<UserService>());
        services.AddScoped<IInstitutionService, InstitutionService>();
        services.AddScoped<IExchangeService, ExchangeService>();
        services.AddScoped<ILearningAgreementService, LearningAgreementService>();
        services.AddScoped<IRecognitionService, RecognitionService>();
        services.AddScoped<IMappingSchemeService, MappingSchemeService>();
        services.AddScoped<ICoordinatorService, CoordinatorService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
