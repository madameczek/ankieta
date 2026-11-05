using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Http;
using Polls.Api.Common.Results;
using Polls.Api.Infrastructure;
using Polls.Api.Users.Core;

namespace Polls.Api.Users.Infrastructure;

/// <summary>Section <c>Session</c>.</summary>
internal sealed class SessionOptions
{
    public const string Section = "Session";

    /// <summary>Secure flag on the cookie; false only for local http (Development, tests) (D44).</summary>
    public bool SecureCookie { get; init; } = true;
}

/// <summary>
/// D10/D44: the current user is carried by ASP.NET's cookie scheme – identity selection, not a login.
/// Persistent, 1 year sliding, HttpOnly, SameSite=Lax, protected by Data Protection with keys in the DB (D22).
/// </summary>
internal static class SessionCookie
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string CookieName = "polls_session";
    public const string ApplicationName = "polls";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    public static IServiceCollection AddPollsSession(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SessionOptions>(configuration.GetSection(SessionOptions.Section));

        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToDbContext<PollsDbContext>();

        services.AddAuthentication(Scheme).AddCookie(Scheme, o =>
        {
            o.Cookie.Name = CookieName;
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.ExpireTimeSpan = Lifetime;
            o.SlidingExpiration = true;

            // API: no redirects, ProblemDetails instead (D11)
            o.Events.OnRedirectToLogin = ctx => WriteProblemAsync(ctx.HttpContext, Error.Unauthorized(UserErrors.SessionRequired));
            o.Events.OnRedirectToAccessDenied = ctx => WriteProblemAsync(ctx.HttpContext, Error.Unauthorized(UserErrors.SessionRequired));

            // A cookie of a user that doesn't exist (e.g. DB reset) means visitor; the cookie is cleared.
            o.Events.OnValidatePrincipal = async ctx =>
            {
                var userId = ctx.Principal?.CurrentUserId();
                var db = ctx.HttpContext.RequestServices.GetRequiredService<PollsDbContext>();
                if (userId is null || !await db.Users.AnyAsync(u => u.Id == userId, ctx.HttpContext.RequestAborted))
                {
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(Scheme);
                }
            };
        });

        // Secure flag read when options are built, so test hosts can override it.
        services.AddOptions<CookieAuthenticationOptions>(Scheme)
            .Configure<Microsoft.Extensions.Options.IOptions<SessionOptions>>((o, session) =>
                o.Cookie.SecurePolicy = session.Value.SecureCookie ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest);

        services.AddAuthorization();

        return services;
    }

    public static Task SignInUserAsync(this HttpContext http, int userId, string name)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)), new Claim(ClaimTypes.Name, name)],
            Scheme);

        return http.SignInAsync(Scheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
    }

    public static Task SignOutUserAsync(this HttpContext http) => http.SignOutAsync(Scheme);

    public static int? CurrentUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>For endpoints behind <c>RequireAuthorization</c>, where a current user is guaranteed.</summary>
    public static int RequiredUserId(this ClaimsPrincipal principal) =>
        principal.CurrentUserId() ?? throw new InvalidOperationException("No current user on an endpoint that requires one.");

    private static Task WriteProblemAsync(HttpContext http, Error error) => error.ToProblem().ExecuteAsync(http);
}
