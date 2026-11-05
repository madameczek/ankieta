using Serilog;
using Serilog.Sinks.Graylog;

namespace Polls.Api.Common.Logging;

internal static class LoggingSetup
{
    public const string ApplicationName = "polls-api";

    /// <summary>Serilog to console and Graylog in every environment (D16).</summary>
    public static WebApplicationBuilder AddPollsLogging(this WebApplicationBuilder builder)
    {
        var graylog = builder.Configuration.GetSection(GraylogSettings.Section).Get<GraylogSettings>() ?? new GraylogSettings();
        var environment = builder.Environment;

        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", ApplicationName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName)
                .WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(graylog.Host))
            {
                logger.WriteTo.Graylog(new GraylogSinkOptions
                {
                    HostnameOrAddress = graylog.Host,
                    Port = graylog.Port,
                    TransportType = graylog.Transport,
                    Facility = ApplicationName,
                });
            }
        });

        return builder;
    }
}
