namespace Polls.Api.Thresholds.Domain;

internal enum ThresholdState
{
    Ok,
    Warning,
    Locked,
}

internal sealed record ThresholdLimits(int Warning, int Lockout);

/// <summary>D14: evaluated before inserting one more row of a resource.</summary>
internal static class ThresholdPolicy
{
    /// <param name="currentCount">Rows that exist now, soft-deleted ones included.</param>
    /// <param name="limits">Warning and lockout levels for the resource.</param>
    public static ThresholdState Evaluate(int currentCount, ThresholdLimits limits) =>
        currentCount >= limits.Lockout ? ThresholdState.Locked
        : currentCount + 1 >= limits.Warning ? ThresholdState.Warning
        : ThresholdState.Ok;
}
