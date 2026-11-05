using Polls.Api.Thresholds.Domain;

namespace Polls.Api.Thresholds.Core;

/// <summary>Section <c>Thresholds</c> (D14).</summary>
internal sealed class ThresholdOptions
{
    public const string Section = "Thresholds";

    public ThresholdLimits Users { get; init; } = new(Warning: 2000, Lockout: 3000);

    public ThresholdLimits Polls { get; init; } = new(Warning: 3000, Lockout: 5000);
}
