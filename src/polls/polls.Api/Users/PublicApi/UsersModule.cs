using Polls.Api.Users.Infrastructure;

namespace Polls.Api.Users.PublicApi;

internal static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration) =>
        services.AddPollsSession(configuration);
}
