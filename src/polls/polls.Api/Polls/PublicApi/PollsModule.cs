namespace Polls.Api.Polls.PublicApi;

internal static class PollsModule
{
    /// <summary>Handlers, validators and endpoints are registered by assembly scanning; module-specific services go here.</summary>
    public static IServiceCollection AddPollsModule(this IServiceCollection services, IConfiguration configuration) => services;
}
