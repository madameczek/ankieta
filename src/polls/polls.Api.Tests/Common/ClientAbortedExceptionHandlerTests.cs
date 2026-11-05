using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Polls.Api.Common.Http;

namespace Polls.Api.Tests.Common;

public sealed class ClientAbortedExceptionHandlerTests
{
    private readonly ClientAbortedExceptionHandler _handler = new(NullLogger<ClientAbortedExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandle_ClientAborted_HandlesAs499()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var http = new DefaultHttpContext { RequestAborted = aborted.Token };

        var handled = await _handler.TryHandleAsync(http, new InvalidOperationException("query cancelled"), TestContext.Current.CancellationToken);

        handled.Should().BeTrue();
        http.Response.StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task TryHandle_ClientStillConnected_LeavesItToTheDefaultHandler()
    {
        var http = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(http, new InvalidOperationException("real failure"), TestContext.Current.CancellationToken);

        handled.Should().BeFalse();
    }
}
