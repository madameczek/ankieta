using System.Net;
using AwesomeAssertions;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Common;

public sealed class HostEndpointsTests(SqlServerFixture database) : IAsyncDisposable
{
    private readonly PollsApiFactory _factory = new(database.ConnectionString);

    [Theory]
    [InlineData("/api/health/live")]
    [InlineData("/api/health/ready")]
    [InlineData("/api/openapi/v1.json")]
    [InlineData("/api/docs")]
    public async Task Get_HostEndpoint_ReturnsOk(string path)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_UnknownRoute_ReturnsProblemDetails()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        (await response.ReadProblemCodeAsync(TestContext.Current.CancellationToken)).Should().Be("route.not_found");
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
