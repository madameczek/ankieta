using System.Net;
using System.Text.Json;
using Polls.Api.Common.Email;

namespace Polls.Api.Tests.Common.Email;

/// <summary>Fake mail API: records the last request and answers with a fixed status.</summary>
internal sealed class RecordingHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    public HttpMethod? Method { get; private set; }

    public Uri? Uri { get; private set; }

    public string? ApiKey { get; private set; }

    public string? Body { get; private set; }

    public JsonElement Json => JsonDocument.Parse(Body!).RootElement;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        Uri = request.RequestUri;
        ApiKey = request.Headers.GetValues(HttpEmailSender.ApiKeyHeader).Single();
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(status);
    }
}
