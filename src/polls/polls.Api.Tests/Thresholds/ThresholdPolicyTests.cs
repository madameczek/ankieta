using AwesomeAssertions;
using Polls.Api.Thresholds.Domain;

namespace Polls.Api.Tests.Thresholds;

public sealed class ThresholdPolicyTests
{
    // Limits as in the spec for users: warning 2000, lockout 3000. ThresholdState is internal, so it's passed by name.
    [Theory]
    [InlineData(0, nameof(ThresholdState.Ok))]
    [InlineData(1998, nameof(ThresholdState.Ok))]
    [InlineData(1999, nameof(ThresholdState.Warning))]
    [InlineData(2999, nameof(ThresholdState.Warning))]
    [InlineData(3000, nameof(ThresholdState.Locked))]
    [InlineData(3500, nameof(ThresholdState.Locked))]
    public void Evaluate_CountBeforeInsert_ReturnsState(int currentCount, string expected)
    {
        ThresholdPolicy.Evaluate(currentCount, new ThresholdLimits(Warning: 2000, Lockout: 3000))
            .Should().Be(Enum.Parse<ThresholdState>(expected));
    }
}
