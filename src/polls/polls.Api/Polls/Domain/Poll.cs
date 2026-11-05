namespace Polls.Api.Polls.Domain;

internal sealed class Poll
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Changes on poll and question edits only (D34).</summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>Soft delete (D8).</summary>
    public DateTime? DeletedAt { get; set; }

    public List<Question> Questions { get; } = [];

    public List<Participant> Participants { get; } = [];
}
