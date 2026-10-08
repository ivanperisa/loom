using System.Security.Claims;
using Loom.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Loom.Api.Extensions;

/// <summary>
/// Development-only login that skips Google: signs in the auth cookie with the same claims Google would give,
/// so user sync, roles and permissions run exactly as in production.
/// </summary>
public static class DevAuthExtensions
{
    private const string EnabledKey = "DevAuth:Enabled";

    public static bool IsDevAuthEnabled(this WebApplicationBuilder builder)
    {
        var enabled = builder.Configuration.GetValue<bool>(EnabledKey);
        if (enabled && !builder.Environment.IsDevelopment())
            throw new InvalidOperationException($"{EnabledKey} must never be enabled outside the Development environment.");
        return enabled;
    }

    public static IEndpointRouteBuilder MapDevAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth/dev").AllowAnonymous();

        group.MapGet("/users", async (IAppDbContext db, CancellationToken ct) =>
            await db.Users
                .AsNoTracking()
                .Where(u => u.Email != "")
                .OrderBy(u => u.Role).ThenBy(u => u.Name)
                .Select(u => new DevUserResponse(u.Email, u.Name, u.Role.ToString(), u.IsOnboarded))
                .ToListAsync(ct));

        group.MapPost("/login", async (DevLoginRequest request, IAppDbContext db, HttpContext http, CancellationToken ct) =>
        {
            var email = request.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email)) return Results.BadRequest();

            // Existing users keep their external id; unknown emails behave like a first Google login.
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user?.ExternalId ?? $"dev:{email}"),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, user?.Name ?? request.Name ?? email),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record DevLoginRequest(string? Email, string? Name);
internal sealed record DevUserResponse(string Email, string Name, string Role, bool IsOnboarded);
