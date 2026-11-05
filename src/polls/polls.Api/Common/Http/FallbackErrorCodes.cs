using System.Globalization;

namespace Polls.Api.Common.Http;

/// <summary>
/// Every ProblemDetails carries a stable <c>code</c> (D40). Business errors set their own; responses produced by the
/// framework (malformed body, unknown route, unhandled exception) get one from their status here.
/// </summary>
internal static class FallbackErrorCodes
{
    public const string RequestInvalid = "request.invalid";
    public const string RouteNotFound = "route.not_found";
    public const string MethodNotAllowed = "method.not_allowed";
    public const string ServerError = "server.error";

    public static string For(int? status) => status switch
    {
        StatusCodes.Status400BadRequest or StatusCodes.Status415UnsupportedMediaType => RequestInvalid,
        StatusCodes.Status404NotFound => RouteNotFound,
        StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
        >= StatusCodes.Status500InternalServerError => ServerError,
        _ => "http." + (status ?? 0).ToString(CultureInfo.InvariantCulture),
    };

    public static void AddCodeIfMissing(ProblemDetailsContext context)
    {
        if (!context.ProblemDetails.Extensions.ContainsKey(ResultHttpExtensions.CodeExtension))
        {
            context.ProblemDetails.Extensions[ResultHttpExtensions.CodeExtension] = For(context.ProblemDetails.Status);
        }
    }
}
