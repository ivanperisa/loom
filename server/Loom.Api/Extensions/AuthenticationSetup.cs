using Loom.Api.Options;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Loom.Api.Extensions;

public static class AuthenticationSetup
{
    public const string GoogleScheme = "GoogleOidc";

    /// <summary>Separate cookie for someone who opened an access link. Never mixed with the login cookie.</summary>
    public const string GuestScheme = "Guest";
    public const string GuestLinkClaim = "access_link";

    /// <summary>
    /// Cookie session, signed in through Google (OpenID Connect). Google is optional in Development,
    /// where the dev login is used instead.
    /// </summary>
    public static void AddLoomAuthentication(this WebApplicationBuilder builder)
    {
        var google = builder.Configuration.GetSection(GoogleOptions.Section).Get<GoogleOptions>() ?? new GoogleOptions();
        if (!google.IsConfigured && !builder.Environment.IsDevelopment())
            throw new InvalidOperationException("Google OAuth is not configured. Set Google:ClientId and Google:ClientSecret.");

        var authentication = builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // API calls without a session get a 401, never a redirect to Google; /auth/login challenges Google itself.
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options => ConfigureApiCookie(options, builder.Environment))
            .AddCookie(GuestScheme, options =>
            {
                ConfigureApiCookie(options, builder.Environment);
                options.Cookie.Name = "loom_guest";
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.SlidingExpiration = true;
            });

        if (google.IsConfigured)
        {
            authentication.AddOpenIdConnect(GoogleScheme, options =>
            {
                options.Authority = "https://accounts.google.com";
                options.ClientId = google.ClientId;
                options.ClientSecret = google.ClientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = false;   // keeps the cookie small (nginx proxy buffers)
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.Prompt = context.Properties.Parameters.TryGetValue("prompt", out var prompt)
                        ? prompt!.ToString()
                        : "select_account";
                    return Task.CompletedTask;
                };
            });
        }

        builder.Services.AddAuthorization();
    }

    private static void ConfigureApiCookie(CookieAuthenticationOptions options, IWebHostEnvironment environment)
    {
        options.Cookie.HttpOnly = true;
        if (environment.IsDevelopment())
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
        // An API answers with status codes instead of redirecting to a login page.
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
    }
}
