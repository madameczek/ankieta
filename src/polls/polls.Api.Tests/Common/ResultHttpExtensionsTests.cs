using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Polls.Api.Common.Http;
using Polls.Api.Common.Results;

namespace Polls.Api.Tests.Common;

public sealed class ResultHttpExtensionsTests
{
    // ErrorType is internal, so cases pass its name (public test methods can't take internal types).
    [Theory]
    [InlineData(nameof(ErrorType.Unauthorized), StatusCodes.Status401Unauthorized)]
    [InlineData(nameof(ErrorType.Forbidden), StatusCodes.Status403Forbidden)]
    [InlineData(nameof(ErrorType.NotFound), StatusCodes.Status404NotFound)]
    [InlineData(nameof(ErrorType.Conflict), StatusCodes.Status409Conflict)]
    [InlineData(nameof(ErrorType.TooManyRequests), StatusCodes.Status429TooManyRequests)]
    [InlineData(nameof(ErrorType.Unavailable), StatusCodes.Status503ServiceUnavailable)]
    public void ToProblem_BusinessError_ReturnsProblemWithStatusAndCode(string type, int expectedStatus)
    {
        var error = new Error(Enum.Parse<ErrorType>(type), "some.code");

        var result = error.ToProblem();

        var problem = result.Should().BeOfType<ProblemHttpResult>().Subject.ProblemDetails;
        problem.Status.Should().Be(expectedStatus);
        problem.Extensions.Should().ContainKey(ResultHttpExtensions.CodeExtension)
            .WhoseValue.Should().Be("some.code");
    }

    [Fact]
    public void ToProblem_ValidationError_ReturnsValidationProblemWithFieldCodes()
    {
        var error = Error.Validation(new Dictionary<string, string[]> { ["name"] = ["name.required"] });

        var result = error.ToProblem();

        var problem = result.Should().BeOfType<ValidationProblem>().Subject.ProblemDetails;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Errors.Should().ContainKey("name").WhoseValue.Should().Equal("name.required");
        problem.Extensions[ResultHttpExtensions.CodeExtension].Should().Be(Error.ValidationFailedCode);
    }

    [Fact]
    public void ToHttpResult_Success_CallsOnSuccessWithValue()
    {
        Result<int> result = 42;

        var http = result.ToHttpResult(TypedResults.Ok);

        http.Should().BeOfType<Ok<int>>().Which.Value.Should().Be(42);
    }

    [Fact]
    public void ToHttpResult_Failure_ReturnsProblem()
    {
        Result<int> result = Error.NotFound("poll.not_found");

        var http = result.ToHttpResult(TypedResults.Ok);

        http.Should().BeOfType<ProblemHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Value_Failure_Throws()
    {
        Result<int> result = Error.Conflict("x");

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }
}
