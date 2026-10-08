using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Loom.Api.Errors;
using Loom.Api.Middleware;
using Loom.Api.Options;
using Loom.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace Loom.Api.Extensions;

public static class ApiSetup
{
    public const string CorsPolicy = "CorsPolicy";
    public const string AuthRateLimit = "auth";

    public static void AddLoomApi(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddOptions<FrontendOptions>().BindConfiguration(FrontendOptions.Section).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<FrontendUrls>();

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()
                .WithExposedHeaders("Content-Disposition")));   // file names of downloads

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(AuthRateLimit, limiter =>
            {
                limiter.PermitLimit = 10;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiter.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: ["ready"]);
    }

    public static void UseLoomApi(this WebApplication app, bool devAuthEnabled)
    {
        // Behind nginx: trust X-Forwarded-* and honour the deployment's path prefix.
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        });
        if (app.Configuration["PathBase"] is { Length: > 0 } pathBase) app.UsePathBase(pathBase);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("../swagger/v1/swagger.json", "Loom API V1");
                c.RoutePrefix = "docs";
            });
        }

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.Append("X-Content-Type-Options", "nosniff");
            headers.Append("X-Frame-Options", "DENY");
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.Append("X-XSS-Protection", "0");
            headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            await next();
        });

        app.UseExceptionHandler();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.UseMiddleware<UserSyncMiddleware>();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapControllers();
        if (devAuthEnabled) app.MapDevAuth();

        app.MapHealthChecks("/healthz", new() { Predicate = _ => false });                       // process is up
        app.MapHealthChecks("/healthz/ready", new() { Predicate = c => c.Tags.Contains("ready") }); // and the database answers
    }
}
