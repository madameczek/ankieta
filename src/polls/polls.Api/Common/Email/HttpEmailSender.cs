using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Polls.Api.Common.Email;

/// <summary>Sends email through the LAN mail HTTP API (D17): POST JSON to <see cref="EmailApiOptions.Url"/>, key in <c>x-api-key</c>.</summary>
internal sealed class HttpEmailSender(IHttpClientFactory httpClientFactory, IOptions<EmailApiOptions> options, ILogger<HttpEmailSender> logger) : IEmailSender
{
    public const string ClientName = "EmailApi";
    public const string ApiKeyHeader = "x-api-key";

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var subject = settings.FormatSubject(message.Subject);
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Url)
        {
            Content = JsonContent.Create(new SendEmailRequest(
                message.To,
                settings.From,
                subject,
                $"<html><body><p>{WebUtility.HtmlEncode(message.Body)}</p></body></html>",
                settings.Signature)),
        };
        request.Headers.Add(ApiKeyHeader, settings.ApiKey);

        using var response = await httpClientFactory.CreateClient(ClientName).SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Email sent. To {EmailTo}, subject {EmailSubject}", message.To, subject);
    }

    /// <summary>Request body of the mail API (PascalCase names).</summary>
    internal sealed record SendEmailRequest(
        [property: JsonPropertyName("DirectAddress")] string DirectAddress,
        [property: JsonPropertyName("From")] string From,
        [property: JsonPropertyName("Subject")] string Subject,
        [property: JsonPropertyName("Content")] string Content,
        [property: JsonPropertyName("Signature")] string Signature);
}
