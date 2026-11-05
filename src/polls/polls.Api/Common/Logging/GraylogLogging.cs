using Serilog;
using Serilog.Events;
using Serilog.Sinks.Graylog;
using Serilog.Sinks.Graylog.Core.Transport;

namespace Polls.Api.Common.Logging;

/// <summary>Section <c>Graylog</c>: GELF over UDP; the sink is off while <see cref="Address"/> is empty (local runs without it, tests).</summary>
internal sealed class GraylogOptions
{
    public const string Section = "Graylog";

    /// <summary>Set per environment (user secrets / k8s secret), never committed.</summary>
    public string? Address { get; init; }

    public int Port { get; init; } = 12201;

    /// <summary>Property <c>AppName</c> on every event.</summary>
    public string? AppName { get; init; }

    /// <summary>GELF <c>host</c>; default = machine name (the pod name in Kubernetes).</summary>
    public string? HostName { get; init; }

    /// <summary>Lowest level sent to Graylog; the Serilog section's overrides still apply first.</summary>
    public LogEventLevel MinimumLevel { get; init; } = LogEventLevel.Information;
}

internal static class GraylogLogging
{
    /// <summary>Adds <c>Environment</c> and <c>AppName</c> to every event and, when an address is set, the Graylog sink (D16).</summary>
    public static LoggerConfiguration WriteToGraylog(this LoggerConfiguration log, IConfiguration configuration, IHostEnvironment environment)
    {
        var options = configuration.GetSection(GraylogOptions.Section).Get<GraylogOptions>() ?? new GraylogOptions();

        log.Enrich.WithProperty("Environment", environment.EnvironmentName);
        if (!string.IsNullOrWhiteSpace(options.AppName))
        {
            log.Enrich.WithProperty("AppName", options.AppName);
        }

        if (string.IsNullOrWhiteSpace(options.Address))
        {
            return log;
        }

        return log.WriteTo.Graylog(new GraylogSinkOptions
        {
            HostnameOrAddress = options.Address,
            Port = options.Port,
            TransportType = TransportType.Udp,
            MinimumLogEventLevel = options.MinimumLevel,
            HostnameOverride = string.IsNullOrWhiteSpace(options.HostName) ? Environment.MachineName : options.HostName,
        });
    }
}

/// <summary>Level of Serilog's one-line-per-request log: failures as Error, probe calls hidden, the rest Information.</summary>
internal static class RequestLogLevel
{
    public static LogEventLevel Get(HttpContext http, double elapsedMs, Exception? ex) =>
        ex is not null || http.Response.StatusCode >= StatusCodes.Status500InternalServerError ? LogEventLevel.Error
        // k8s probes call /api/health every few seconds – logged only when they fail (above)
        : http.Request.Path.StartsWithSegments(RouteConsts.Health) ? LogEventLevel.Verbose
        : LogEventLevel.Information;
}
