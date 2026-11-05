using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polls.Api.Common.Email;

namespace Polls.Api.Tests.Common.Email;

public sealed class EmailSetupTests
{
    private const string Url = "http://mail.example.test/api/email/send";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(Url, "polls@example.test", "admin@example.test", typeof(HttpEmailSender))]
    [InlineData("from-secrets", "polls@example.test", "admin@example.test", typeof(LoggingEmailSender))]
    [InlineData(Url, "from-secrets", "admin@example.test", typeof(LoggingEmailSender))]
    [InlineData(Url, "polls@example.test", "from-secrets", typeof(LoggingEmailSender))]
    public void AddPollsEmail_Settings_SelectsSender(string url, string from, string to, Type expected)
    {
        using var provider = Provider(Valid(url, from, to));

        provider.GetRequiredService<IEmailSender>().Should().BeOfType(expected);
    }

    [Theory]
    [InlineData("polls", "Polls-API", "polls")]
    [InlineData("", "Polls-API", "Polls-API")]
    [InlineData("", "", null)]
    public void AddPollsEmail_SubjectTag_FallsBackToGraylogAppName(string tag, string appName, string? expected)
    {
        using var provider = Provider(new() { ["EmailApi:SubjectTag"] = tag, ["Graylog:AppName"] = appName });

        Options(provider).SubjectTag.Should().Be(expected);
    }

    [Fact]
    public async Task AddPollsEmail_SubjectTagAndAppName_SendsSubjectWithConfiguredTag()
    {
        using var handler = new RecordingHandler();
        var settings = Valid(Url, "polls@example.test", "admin@example.test");
        settings["EmailApi:SubjectTag"] = "polls";
        settings["Graylog:AppName"] = "Polls-API";
        using var provider = Provider(settings, handler);

        await provider.GetRequiredService<IEmailSender>().SendAsync(new EmailMessage("admin@example.test", "Subject", "b"), Ct);

        handler.Json.GetProperty("Subject").GetString().Should().Be("[polls] Subject");
    }

    [Fact]
    public void AddPollsEmail_ApiKeyWithNewline_IsTrimmed()
    {
        using var provider = Provider(new() { ["EmailApi:ApiKey"] = "secret-key\n" });

        Options(provider).ApiKey.Should().Be("secret-key");
    }

    [Fact]
    public void AddPollsEmail_TimeoutSeconds_SetsClientTimeout()
    {
        using var provider = Provider(new() { ["EmailApi:TimeoutSeconds"] = "7" });

        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpEmailSender.ClientName);

        client.Timeout.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void AddPollsEmail_TimeoutNotPositive_FailsValidation(string timeout)
    {
        using var provider = Provider(new() { ["EmailApi:TimeoutSeconds"] = timeout });

        var read = () => Options(provider);

        read.Should().Throw<OptionsValidationException>();
    }

    private static Dictionary<string, string?> Valid(string url, string from, string to) => new()
    {
        ["EmailApi:Url"] = url,
        ["EmailApi:ApiKey"] = "secret-key",
        ["EmailApi:From"] = from,
        ["EmailApi:To"] = to,
    };

    private static EmailApiOptions Options(ServiceProvider provider) =>
        provider.GetRequiredService<IOptions<EmailApiOptions>>().Value;

    private static ServiceProvider Provider(Dictionary<string, string?> settings, RecordingHandler? handler = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging().AddPollsEmail(configuration);
        if (handler is not null)
        {
            services.AddHttpClient(HttpEmailSender.ClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        return services.BuildServiceProvider();
    }
}
