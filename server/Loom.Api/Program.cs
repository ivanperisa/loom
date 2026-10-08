using Loom.Api.Extensions;
using Loom.Api.Middleware;
using Loom.Application;
using Loom.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using NLog;
using NLog.Web;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseNLog(new NLogAspNetCoreOptions() { RemoveLoggerFactoryFilter = false });
var logger = LogManager.Setup().GetCurrentClassLogger();
try
{
  logger.Debug("init main");

  builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen();

  builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection"));
  builder.Services.AddApplication();

  var allowedOrigins = builder.Configuration
      .GetSection("Cors:AllowedOrigins")
      .Get<string[]>() ?? [];

  builder.Services.AddCors(options =>
  {
    options.AddPolicy("CorsPolicy", policy =>
  {
        policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
      });
  });

  var googleClientId = builder.Configuration["Google:ClientId"];
  var googleClientSecret = builder.Configuration["Google:ClientSecret"];
  var googleConfigured = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);
  if (!googleConfigured && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException(
        "Google OAuth is not configured. Set Google:ClientId and Google:ClientSecret.");

  var devAuthEnabled = builder.IsDevAuthEnabled();

  var authentication = builder.Services.AddAuthentication(options =>
  {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = googleConfigured ? "GoogleOidc" : CookieAuthenticationDefaults.AuthenticationScheme;
  })
      .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
      {
        options.Cookie.HttpOnly = true;
        if (builder.Environment.IsDevelopment())
        {
          // Local dev runs over plain http behind the Vite proxy (same origin).
          options.Cookie.SameSite = SameSiteMode.Lax;
          options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        }
        else
        {
          options.Cookie.SameSite = SameSiteMode.None;
          options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
        options.Events.OnRedirectToLogin = ctx =>
      {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
          };
        options.Events.OnRedirectToAccessDenied = ctx =>
      {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
          };
      });

  if (googleConfigured)
  {
    authentication.AddOpenIdConnect("GoogleOidc", options =>
      {
        options.Authority = "https://accounts.google.com";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = false; //or increase in nginx conf  proxy_buffer_size 16k;proxy_buffers 8 16k; proxy_busy_buffers_size 32k;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.Events.OnRedirectToIdentityProvider = context =>
        {
          if (context.Properties.Parameters.TryGetValue("prompt", out var prompt))
            context.ProtocolMessage.Prompt = prompt!.ToString();
          else
            context.ProtocolMessage.Prompt = "select_account";

          return Task.CompletedTask;
        };
      });
  }

  builder.Services.AddAuthorization();

  builder.Services.AddRateLimiter(options =>
  {
    options.AddFixedWindowLimiter("auth", o =>
  {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
      });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
  });

  builder.Services.AddHealthChecks();

  var app = builder.Build();

  #region Needed for nginx and Kestrel 
  app.UseForwardedHeaders(new ForwardedHeadersOptions
  {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto
  });
  string? pathBase = app.Configuration["PathBase"];
  if (!string.IsNullOrWhiteSpace(pathBase))
  {
    app.UsePathBase(pathBase);
  }
  #endregion

  if (app.Environment.IsDevelopment())
  {
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
      c.SwaggerEndpoint("../swagger/v1/swagger.json", "Loom API V1");
      c.RoutePrefix = "docs";
    });
  }

  app.Use(async (context, next) =>
  {
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-XSS-Protection", "0");
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
  });

  app.UseMiddleware<ExceptionHandlingMiddleware>();
  app.UseCors("CorsPolicy");
  app.UseAuthentication();
  app.UseMiddleware<UserSyncMiddleware>();
  app.UseAuthorization();
  app.UseRateLimiter();

  app.MapControllers();
  if (devAuthEnabled)
    app.MapDevAuth();

  app.MapHealthChecks("/healthz");

  app.Run();
}
catch (Exception exception)
{
  // NLog: catch setup errors
  logger.Error(exception, "Stopped program because of exception");
  throw;
}
finally
{
  // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
  NLog.LogManager.Shutdown();
}

