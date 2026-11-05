using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Polls.Api.Common.Email;

namespace Polls.Api.Tests.Common.Email;

public sealed class HttpEmailSenderTests
{
    private const string Url = "http://mail.example.test/api/email/send";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SendAsync_ValidMessage_PostsContractWithApiKeyTaggedSubjectAndEncodedHtml()
    {
        using var handler = new RecordingHandler();

        await Sender(handler, subjectTag: "polls").SendAsync(new EmailMessage("admin@example.test", "Subject", "a < b & c"), Ct);

        handler.Method.Should().Be(HttpMethod.Post);
        handler.Uri.Should().Be(new Uri(Url));
        handler.ApiKey.Should().Be("secret-key");
        var json = handler.Json;
        json.GetProperty("DirectAddress").GetString().Should().Be("admin@example.test");
        json.GetProperty("From").GetString().Should().Be("polls@example.test");
        json.GetProperty("Subject").GetString().Should().Be("[polls] Subject");
        json.GetProperty("Content").GetString().Should().Be("<html><body><p>a &lt; b &amp; c</p></body></html>");
        json.GetProperty("Signature").GetString().Should().Be("Polls");
    }

    [Fact]
    public async Task SendAsync_NoSubjectTag_SendsSubjectUnchanged()
    {
        using var handler = new RecordingHandler();

        await Sender(handler, subjectTag: null).SendAsync(new EmailMessage("admin@example.test", "Subject", "b"), Ct);

        handler.Json.GetProperty("Subject").GetString().Should().Be("Subject");
    }

    [Fact]
    public async Task SendAsync_ApiReturnsError_Throws()
    {
        using var handler = new RecordingHandler(HttpStatusCode.BadRequest);

        var send = () => Sender(handler, subjectTag: null).SendAsync(new EmailMessage("a@example.test", "s", "b"), Ct);

        await send.Should().ThrowAsync<HttpRequestException>();
    }

    private static HttpEmailSender Sender(RecordingHandler handler, string? subjectTag)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpEmailSender.ClientName).Returns(_ => new HttpClient(handler, disposeHandler: false));

        return new HttpEmailSender(
            factory,
            Options.Create(new EmailApiOptions
            {
                Url = Url,
                ApiKey = "secret-key",
                From = "polls@example.test",
                Signature = "Polls",
                SubjectTag = subjectTag,
            }),
            NullLogger<HttpEmailSender>.Instance);
    }
}
