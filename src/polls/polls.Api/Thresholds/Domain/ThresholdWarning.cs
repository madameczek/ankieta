namespace Polls.Api.Thresholds.Domain;

/// <summary>Records that the warning email for a resource was sent, so it goes out once (D14).</summary>
internal sealed class ThresholdWarning
{
    public const string Users = "users";
    public const string Polls = "polls";
    public const int ResourceMaxLength = 20;

    public required string Resource { get; set; }

    public DateTime SentAt { get; set; }
}
