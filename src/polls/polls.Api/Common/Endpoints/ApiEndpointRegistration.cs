using Polls.Api.Common.Handlers;

namespace Polls.Api.Common.Endpoints;

internal static class ApiEndpointRegistration
{
    public static IServiceCollection RegisterApiEndpointsFromAssemblyContaining<TMarker>(this IServiceCollection services)
    {
        foreach (var type in HandlerRegistration.ConcreteTypesOf<IApiEndpoint>(typeof(TMarker).Assembly))
        {
            services.AddSingleton(typeof(IApiEndpoint), type);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        foreach (var endpoint in app.ServiceProvider.GetServices<IApiEndpoint>())
        {
            endpoint.Map(app);
        }

        return app;
    }
}
