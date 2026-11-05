using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Polls.Api.Polls.Domain;
using Polls.Api.Users.Domain;

namespace Polls.Api.Infrastructure;

/// <summary>
/// Sets timestamps centrally (D34): created/modified on users, polls, questions, answers.
/// Any added or modified question (soft delete included) also bumps its poll's <c>modified_at</c>;
/// answers and assignments don't.
/// </summary>
internal sealed class TimestampInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var touchedPollIds = new HashSet<int>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            var added = entry.State == EntityState.Added;
            var modified = entry.State == EntityState.Modified;

            switch (entry.Entity)
            {
                case User user when added:
                    user.CreatedAt = now;
                    break;
                case Poll poll when added:
                    poll.CreatedAt = now;
                    poll.ModifiedAt = now;
                    break;
                case Poll poll when modified:
                    poll.ModifiedAt = now;
                    break;
                case Question question when added || modified:
                    question.ModifiedAt = now;
                    if (question.PollId != 0)
                    {
                        touchedPollIds.Add(question.PollId);
                    }

                    break;
                case Answer answer when added || modified:
                    answer.ModifiedAt = now;
                    break;
            }
        }

        foreach (var pollId in touchedPollIds)
        {
            TouchPoll(context, pollId, now);
        }
    }

    private static void TouchPoll(DbContext context, int pollId, DateTime now)
    {
        var poll = context.ChangeTracker.Entries<Poll>().FirstOrDefault(e => e.Entity.Id == pollId)?.Entity;
        if (poll is null)
        {
            // Not loaded: attach a stub and update only modified_at.
            poll = new Poll { Id = pollId, Title = string.Empty };
            context.Attach(poll);
        }

        var entry = context.Entry(poll);
        if (entry.State == EntityState.Added)
        {
            return;
        }

        entry.Property(p => p.ModifiedAt).CurrentValue = now;
        entry.Property(p => p.ModifiedAt).IsModified = true;
    }
}
