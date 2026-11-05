using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Polls.Api.Common.Email;
using Polls.Api.Common.Endpoints;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Http;
using Polls.Api.Common.Logging;
using Polls.Api.Common.RateLimiting;
using Scalar.AspNetCore;
using Serilog;

namespace Polls.Api.Common;

internal static class CommonSetup
{
    public static IServiceCollection AddPollsCommon(this IServiceCollection services, IConfiguration configuration)
    {
        ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        services.AddProblemDetails(o => o.CustomizeProblemDetails = FallbackErrorCodes.AddCodeIfMissing);
        services.AddExceptionHandler<ClientAbortedExceptionHandler>();
        // Malformed or missing bodies become a 400 ProblemDetails (code request.invalid) in every environment.
        services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);
        services.AddOpenApi();
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new UtcDateTimeConverter()));
        services.AddHealthChecks();
        services.AddPollsRateLimiting(configuration);
        services.AddPollsEmail(configuration);
        services.AddValidatorsFromAssemblyContaining<Marker>(includeInternalTypes: true);
        services.RegisterHandlersFromAssemblyContaining<Marker>();
        services.RegisterApiEndpointsFromAssemblyContaining<Marker>();

        return services;
    }

    public static WebApplication UsePollsCommon(this WebApplication app)
    {
        app.LogEmailSender();
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging(o => o.GetLevel = RequestLogLevel.Get);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapOpenApi(RouteConsts.OpenApi);
        app.MapScalarApiReference(RouteConsts.Docs, options => options.OpenApiRoutePattern = RouteConsts.OpenApi);

        app.MapHealthChecks(RouteConsts.HealthLive, new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks(RouteConsts.HealthReady, new HealthCheckOptions { Predicate = c => c.Tags.Contains(RouteConsts.ReadyTag) });

        app.MapApiEndpoints();

        return app;
    }

    /// <summary>Assembly marker for scanning.</summary>
    private sealed class Marker;
}
