using Polls.Api.Users.Domain;

namespace Polls.Api.Polls.Domain;

/// <summary>One user's answer to one question (D3). No row means "not answered" (D31).</summary>
internal sealed class Answer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public int UserId { get; set; }

    public required string Text { get; set; }

    public DateTime ModifiedAt { get; set; }

    public Question Question { get; set; } = null!;

    public User User { get; set; } = null!;
}
