namespace Polls.Api.Infrastructure;

internal static class DbDefaults
{
    /// <summary>DB-side default for timestamps; the app normally sets them (<see cref="TimestampInterceptor"/>).</summary>
    public const string UtcNow = "SYSUTCDATETIME()";
}
