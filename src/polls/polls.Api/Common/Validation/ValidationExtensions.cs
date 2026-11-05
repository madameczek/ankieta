using FluentValidation;
using FluentValidation.Results;
using Polls.Api.Common.Results;

namespace Polls.Api.Common.Validation;

internal static class ValidationExtensions
{
    /// <summary>Validates and returns a validation <see cref="Error"/>, or <c>null</c> when valid.</summary>
    public static async Task<Error?> ValidateToErrorAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        return result.IsValid ? null : result.ToError();
    }

    public static Error ToError(this ValidationResult result)
    {
        var fieldErrors = result.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorCode).Distinct().ToArray());

        return Error.Validation(fieldErrors);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
