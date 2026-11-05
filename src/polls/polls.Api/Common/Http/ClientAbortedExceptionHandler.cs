using Microsoft.AspNetCore.Diagnostics;

namespace Polls.Api.Common.Http;

/// <summary>
/// The client went away (closed the tab, navigated on): whatever the query threw – SqlClient reports a cancelled command
/// as a SqlException – it is not a server error. Logged as information with status 499, no response body.
/// </summary>
internal sealed class ClientAbortedExceptionHandler(ILogger<ClientAbortedExceptionHandler> logger) : IExceptionHandler
{
    public const int ClientClosedRequest = 499;

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!httpContext.RequestAborted.IsCancellationRequested)
        {
            return ValueTask.FromResult(false);
        }

        logger.LogInformation("Request {Method} {Path} aborted by the client", httpContext.Request.Method, httpContext.Request.Path);
        httpContext.Response.StatusCode = ClientClosedRequest;
        return ValueTask.FromResult(true);
    }
}
