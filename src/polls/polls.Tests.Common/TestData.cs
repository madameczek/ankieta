using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;
using Polls.Api.Users.Domain;

namespace Polls.Tests.Common;

/// <summary>Seeds rows with unique names, so tests can share one database.</summary>
internal static class TestData
{
    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 13, 60)];

    public static async Task<User> AddUserAsync(this PollsDbContext db, CancellationToken cancellationToken)
    {
        var user = new User { Name = Unique("user") };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    /// <summary>Poll with its owner row, created together (D4).</summary>
    public static async Task<Poll> AddPollAsync(this PollsDbContext db, User owner, CancellationToken cancellationToken, int questionCount = 0)
    {
        var poll = new Poll { Title = Unique("poll") };
        poll.Participants.Add(new Participant { UserId = owner.Id, IsOwner = true });
        for (var i = 1; i <= questionCount; i++)
        {
            poll.Questions.Add(new Question { SortOrder = i, Text = $"Question {i}" });
        }

        db.Polls.Add(poll);
        await db.SaveChangesAsync(cancellationToken);
        return poll;
    }
}
