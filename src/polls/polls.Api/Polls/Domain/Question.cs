namespace Polls.Api.Polls.Domain;

internal sealed class Question
{
    public int Id { get; set; }

    public int PollId { get; set; }

    /// <summary>Display order, unique among the poll's non-deleted questions (D9).</summary>
    public int SortOrder { get; set; }

    public required string Text { get; set; }

    public DateTime ModifiedAt { get; set; }

    /// <summary>Soft delete (D8).</summary>
    public DateTime? DeletedAt { get; set; }

    public Poll Poll { get; set; } = null!;

    public List<Answer> Answers { get; } = [];
}
