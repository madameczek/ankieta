using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Polls.Tests.Common;

/// <summary>In-memory API host for endpoint tests. Runs as "Testing", so no console/Graylog sinks and no user secrets.</summary>
public sealed class PollsApiFactory : WebApplicationFactory<Program>
{
    public const string TestingEnvironment = "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment(TestingEnvironment);
}
