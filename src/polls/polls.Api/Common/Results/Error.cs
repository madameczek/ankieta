namespace Polls.Api.Common.Results;

internal enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests,
    Unavailable,
}

/// <summary>Business error with a stable, English <see cref="Code"/> (mapped to UI text by the client).</summary>
internal sealed record Error(ErrorType Type, string Code, string? Detail = null)
{
    public const string ValidationFailedCode = "validation.failed";

    /// <summary>Field name → error codes; set only for validation errors.</summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }

    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new(ErrorType.Validation, ValidationFailedCode) { FieldErrors = fieldErrors };

    public static Error Unauthorized(string code) => new(ErrorType.Unauthorized, code);

    public static Error Forbidden(string code) => new(ErrorType.Forbidden, code);

    public static Error NotFound(string code) => new(ErrorType.NotFound, code);

    public static Error Conflict(string code) => new(ErrorType.Conflict, code);

    public static Error TooManyRequests(string code) => new(ErrorType.TooManyRequests, code);

    public static Error Unavailable(string code) => new(ErrorType.Unavailable, code);
}
