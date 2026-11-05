using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Polls.Api.Common.Logging;
using Serilog.Events;

namespace Polls.Api.Tests.Common;

public sealed class RequestLogLevelTests
{
    [Theory]
    [InlineData("/api/polls", 200, LogEventLevel.Information)]
    [InlineData("/api/polls", 404, LogEventLevel.Information)]
    [InlineData("/api/polls", 500, LogEventLevel.Error)]
    [InlineData("/api/health/ready", 200, LogEventLevel.Verbose)]
    [InlineData("/api/health/ready", 503, LogEventLevel.Error)]
    public void Get_PathAndStatus_ReturnsLevel(string path, int status, LogEventLevel expected)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        http.Response.StatusCode = status;

        RequestLogLevel.Get(http, 1, null).Should().Be(expected);
    }

    [Fact]
    public void Get_Exception_ReturnsError()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/health/live";

        RequestLogLevel.Get(http, 1, new InvalidOperationException()).Should().Be(LogEventLevel.Error);
    }
}
