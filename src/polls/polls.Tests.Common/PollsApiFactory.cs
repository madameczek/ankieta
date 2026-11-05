using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Polls.Api.Infrastructure;

namespace Polls.Tests.Common;

/// <summary>
/// In-memory API host on the test database. Runs as "Testing": no user secrets, no Graylog, cookie without Secure (http),
/// rate limits high enough not to interfere. <paramref name="settings"/> override any configuration key.
/// </summary>
public sealed class PollsApiFactory(string connectionString, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    public const string TestingEnvironment = "Testing";

    private static readonly Dictionary<string, string?> Defaults = new()
    {
        ["Session:SecureCookie"] = "false",
        ["RateLimiting:Global:PermitLimit"] = "100000",
        ["RateLimiting:Create:PermitLimit"] = "100000",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment(TestingEnvironment)
            .UseSetting($"ConnectionStrings:{DatabaseSetup.ConnectionStringName}", connectionString);

        foreach (var (key, value) in Defaults.Concat(settings ?? new Dictionary<string, string?>()))
        {
            builder.UseSetting(key, value);
        }
    }
}
