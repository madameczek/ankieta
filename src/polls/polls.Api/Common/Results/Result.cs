using System.Diagnostics.CodeAnalysis;

namespace Polls.Api.Common.Results;

/// <summary>Outcome of an operation without a value.</summary>
internal sealed class Result
{
    private static readonly Result SuccessInstance = new(null);

    private Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => SuccessInstance;

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation returning <typeparamref name="T"/>.</summary>
internal sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value) => _value = value;

    private Result(Error error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Result has no value; error {Error.Code}.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
