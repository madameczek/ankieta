using Microsoft.Extensions.Options;
using Polls.Api.Common.Logging;

namespace Polls.Api.Common.Email;

internal static class EmailSetup
{
    /// <summary>Mail API sender when <see cref="EmailApiOptions.IsConfigured"/>, otherwise the logging stub (local runs, tests).</summary>
    public static IServiceCollection AddPollsEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailApiOptions>()
            .Bind(configuration.GetSection(EmailApiOptions.Section))
            .PostConfigure(o =>
            {
                o.ApiKey = o.ApiKey.Trim();
                if (string.IsNullOrWhiteSpace(o.SubjectTag))
                {
                    var appName = configuration[$"{GraylogOptions.Section}:{nameof(GraylogOptions.AppName)}"];
                    o.SubjectTag = string.IsNullOrWhiteSpace(appName) ? null : appName;
                }
            })
            .Validate(o => o.TimeoutSeconds > 0, "EmailApi:TimeoutSeconds must be greater than 0")
            .ValidateOnStart();

        services.AddHttpClient(HttpEmailSender.ClientName)
            .ConfigureHttpClient((sp, client) =>
                client.Timeout = TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<EmailApiOptions>>().Value.TimeoutSeconds));

        services.AddSingleton<IEmailSender>(CreateSender);

        return services;
    }

    /// <summary>Resolve once at startup, so configuration problems are logged when the app starts.</summary>
    public static WebApplication LogEmailSender(this WebApplication app)
    {
        app.Services.GetRequiredService<IEmailSender>();
        return app;
    }

    private static IEmailSender CreateSender(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<EmailApiOptions>>().Value;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(EmailSetup));

        if (options.IsConfigured)
        {
            return ActivatorUtilities.CreateInstance<HttpEmailSender>(sp);
        }

        logger.LogError(
            "EmailApi settings invalid (Url {EmailUrl}, ApiKey set {EmailApiKeySet}, From {EmailFrom}, To {EmailTo}); emails are only logged",
            options.Url,
            options.HasApiKey,
            options.From,
            options.To);
        return ActivatorUtilities.CreateInstance<LoggingEmailSender>(sp);
    }
}
