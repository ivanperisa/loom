using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Loom.Api.Errors;
using Loom.Api.Middleware;
using Loom.Api.OpenApi;
using Loom.Api.Options;
using Loom.Application.Common.Querying;
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
        // The document is the client's contract: client/openapi.json → TypeScript types (see ApiContractTests).
        services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
            options.UseAllOfToExtendReferenceSchemas();   // keeps "nullable" on enum and object references
            options.CustomSchemaIds(SchemaId);
            options.SchemaFilter<ResponsePropertiesRequiredFilter>();
        });

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

        AddErrorTracking(builder);
    }

    /// <summary>
    /// Optional: unhandled exceptions and logged errors go to Sentry (or a self-hosted GlitchTip, same protocol)
    /// when <c>Sentry:Dsn</c> is set, e.g. <c>Sentry__Dsn=https://key@glitchtip.example/1</c>. Without a DSN nothing is
    /// registered. No personal data: no cookies, headers, request bodies or user emails are sent.
    /// </summary>
    private static void AddErrorTracking(WebApplicationBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(builder.Configuration["Sentry:Dsn"])) return;
        builder.WebHost.UseSentry(options =>
        {
            options.Environment = builder.Environment.EnvironmentName;
            options.SendDefaultPii = false;
            options.MaxRequestBodySize = Sentry.Extensibility.RequestSize.None;
            options.TracesSampleRate = 0;
        });
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

    /// <summary>Readable, stable schema names: <c>PagedResponse&lt;UserListResponse&gt;</c> → <c>UserListResponsePage</c>.</summary>
    private static string SchemaId(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PagedResponse<>)
            ? $"{SchemaId(type.GetGenericArguments()[0])}Page"
            : type.IsGenericType
                ? $"{type.Name[..type.Name.IndexOf('`')]}Of{string.Concat(type.GetGenericArguments().Select(SchemaId))}"
                : type.Name;
}
