using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;
using Polls.Api.Polls.PublicApi;
using Polls.Api.Users.Core;

namespace Polls.Api.Polls.Core;

/// <summary>Owner first, then by name; with each participant's answer count.</summary>
internal sealed class ListParticipantsHandler(PollsDbContext db, PollAccess access) : IHandler
{
    public async Task<Result<IReadOnlyList<ParticipantView>>> HandleAsync(int userId, int pollId, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed.Error;
        }

        var participants = await db.Participants.AsNoTracking()
            .Where(p => p.PollId == pollId)
            .OrderByDescending(p => p.IsOwner).ThenBy(p => p.User.Name)
            .Select(p => new ParticipantView(
                p.UserId,
                p.User.Name,
                p.IsOwner,
                db.Answers.Count(a => a.UserId == p.UserId && a.Question.PollId == pollId)))
            .ToListAsync(cancellationToken);

        return participants;
    }
}

/// <summary>Idempotent: adding an existing participant changes nothing.</summary>
internal sealed class AddParticipantHandler(PollsDbContext db, PollAccess access) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, int participantUserId, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        if (!await db.Users.AnyAsync(u => u.Id == participantUserId, cancellationToken))
        {
            return Error.NotFound(UserErrors.NotFound);
        }

        if (await db.Participants.AnyAsync(p => p.PollId == pollId && p.UserId == participantUserId, cancellationToken))
        {
            return Result.Success();
        }

        db.Participants.Add(new Participant { PollId = pollId, UserId = participantUserId });
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// D25: deletes the assignment; the participant's answers stay but are hidden (results list current participants only).
/// The owner can't be removed. Removing a non-participant is a no-op.
/// </summary>
internal sealed class RemoveParticipantHandler(PollsDbContext db, PollAccess access) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, int participantUserId, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var participant = await db.Participants.SingleOrDefaultAsync(p => p.PollId == pollId && p.UserId == participantUserId, cancellationToken);
        if (participant is null)
        {
            return Result.Success();
        }

        if (participant.IsOwner)
        {
            return Error.Conflict(PollErrors.ParticipantIsOwner);
        }

        db.Participants.Remove(participant);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
