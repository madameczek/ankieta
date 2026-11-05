using Polls.Api.Users.Domain;

namespace Polls.Api.Polls.Domain;

/// <summary>A user assigned to a poll (table <c>user_poll</c>). Exactly one participant per poll is the owner (D4, D5).</summary>
internal sealed class Participant
{
    public int PollId { get; set; }

    public int UserId { get; set; }

    public bool IsOwner { get; set; }

    public Poll Poll { get; set; } = null!;

    public User User { get; set; } = null!;
}
