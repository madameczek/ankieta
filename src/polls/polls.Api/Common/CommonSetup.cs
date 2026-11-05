using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Polls.Api.Common.Endpoints;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Logging;
using Scalar.AspNetCore;
using Serilog;

namespace Polls.Api.Common;

internal static class CommonSetup
{
    public static IServiceCollection AddPollsCommon(this IServiceCollection services)
    {
        ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddHealthChecks();
        services.AddValidatorsFromAssemblyContaining<Marker>(includeInternalTypes: true);
        services.RegisterHandlersFromAssemblyContaining<Marker>();
        services.RegisterApiEndpointsFromAssemblyContaining<Marker>();

        return services;
    }

    public static WebApplication UsePollsCommon(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging(o => o.GetLevel = RequestLogLevel.Get);

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
