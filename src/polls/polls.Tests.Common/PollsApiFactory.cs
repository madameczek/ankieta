using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Polls.Api.Infrastructure;

namespace Polls.Tests.Common;

/// <summary>
/// In-memory API host on the test database. Runs as "Testing": no user secrets, no Graylog.
/// </summary>
public sealed class PollsApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string TestingEnvironment = "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder
            .UseEnvironment(TestingEnvironment)
            .UseSetting($"ConnectionStrings:{DatabaseSetup.ConnectionStringName}", connectionString);
}
