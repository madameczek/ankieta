using Polls.Api.Common.Results;

namespace Polls.Api.Common.Http;

/// <summary>Maps <see cref="Result"/> / <see cref="Error"/> to HTTP results (<c>ProblemDetails</c> with a <c>code</c> extension).</summary>
internal static class ResultHttpExtensions
{
    public const string CodeExtension = "code";

    public static IResult ToHttpResult(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : result.Error.ToProblem();

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();

    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { [CodeExtension] = error.Code };

        if (error.FieldErrors is not null)
        {
            return TypedResults.ValidationProblem(error.FieldErrors, error.Detail, extensions: extensions);
        }

        return TypedResults.Problem(error.Detail, statusCode: error.Type.ToStatusCode(), extensions: extensions);
    }

    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
