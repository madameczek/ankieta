using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polls.Api.Common;

namespace Polls.Api.Infrastructure;

internal static class DatabaseSetup
{
    /// <summary>Connection string name; the value comes from user secrets (dev) or a k8s secret (D36).</summary>
    public const string ConnectionStringName = "Polls";

    public static IServiceCollection AddPollsDatabase(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TimestampInterceptor>();

        services.AddDbContext<PollsDbContext>((sp, options) => options
            .UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName))
            .AddInterceptors(sp.GetRequiredService<TimestampInterceptor>()));

        services.AddHealthChecks().AddDbContextCheck<PollsDbContext>("database", tags: [RouteConsts.ReadyTag]);

        return services;
    }
}
