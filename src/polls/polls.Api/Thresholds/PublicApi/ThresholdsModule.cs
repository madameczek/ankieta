using Polls.Api.Thresholds.Core;

namespace Polls.Api.Thresholds.PublicApi;

internal static class ThresholdsModule
{
    public static IServiceCollection AddThresholdsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ThresholdOptions>(configuration.GetSection(ThresholdOptions.Section));
        return services;
    }
}
