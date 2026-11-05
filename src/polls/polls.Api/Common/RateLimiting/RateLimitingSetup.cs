using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Polls.Api.Common.Http;
using Polls.Api.Common.Results;

namespace Polls.Api.Common.RateLimiting;

internal sealed class RateLimitWindow
{
    public int PermitLimit { get; init; }

    public int WindowSeconds { get; init; } = 60;
}

/// <summary>Section <c>RateLimiting</c> (D15).</summary>
internal sealed class RateLimitSettings
{
    public const string Section = "RateLimiting";

    /// <summary>All API requests, per client IP.</summary>
    public RateLimitWindow Global { get; init; } = new() { PermitLimit = 300 };

    /// <summary>Create endpoints (users, polls), per client IP.</summary>
    public RateLimitWindow Create { get; init; } = new() { PermitLimit = 10 };
}

internal static class RateLimitPolicies
{
    public const string Create = "create";
    public const string RateLimitedCode = "rate_limited";
}

internal static class RateLimitingSetup
{
    public static IServiceCollection AddPollsRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.Section));

        // Client IP from the proxy in front of the API (gateway / nginx). Any proxy is trusted: acceptable for a demo.
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        services.AddRateLimiter(o =>
        {
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                http.Request.Path.StartsWithSegments(RouteConsts.Health)
                    ? RateLimitPartition.GetNoLimiter("health")
                    : FixedWindow("global:" + ClientIp(http), Settings(http).Global));

            o.AddPolicy(RateLimitPolicies.Create, http => FixedWindow("create:" + ClientIp(http), Settings(http).Create));

            o.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await Error.TooManyRequests(RateLimitPolicies.RateLimitedCode).ToProblem().ExecuteAsync(context.HttpContext);
            };
        });

        return services;
    }

    private static RateLimitSettings Settings(HttpContext http) =>
        http.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

    private static string ClientIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static RateLimitPartition<string> FixedWindow(string key, RateLimitWindow window) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = window.PermitLimit,
            Window = TimeSpan.FromSeconds(window.WindowSeconds),
            QueueLimit = 0,
        });
}
