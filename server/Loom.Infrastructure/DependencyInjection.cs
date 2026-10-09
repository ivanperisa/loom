using Loom.Application.Interfaces;
using Loom.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddHybridCache();
        services.AddSingleton<CacheInvalidationInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(WithoutJit(connectionString))
            .AddInterceptors(sp.GetRequiredService<CacheInvalidationInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }

    /// <summary>
    /// PostgreSQL's JIT compiles any query whose estimated cost passes a threshold. Our queries are short and
    /// indexed, so compiling costs more than it saves (15 ms per call, hundreds of ms the first time). Turned off
    /// for this app's connections only; the server setting is untouched. An explicit "jit" in the connection
    /// string wins.
    /// </summary>
    public static string? WithoutJit(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Options?.Contains("jit", StringComparison.OrdinalIgnoreCase) == true) return connectionString;
        builder.Options = string.IsNullOrWhiteSpace(builder.Options) ? "-c jit=off" : $"{builder.Options} -c jit=off";
        return builder.ConnectionString;
    }
}
