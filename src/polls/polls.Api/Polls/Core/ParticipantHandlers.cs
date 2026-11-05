using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Common.Validation;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;
using Polls.Api.Polls.PublicApi;
using Polls.Api.Users.Core;

namespace Polls.Api.Polls.Core;

internal sealed record AddParticipantCommand(int? UserId);

internal sealed class AddParticipantCommandValidator : AbstractValidator<AddParticipantCommand>
{
    public AddParticipantCommandValidator() =>
        RuleFor(c => c.UserId)
            .NotNull().WithErrorCode(PollErrors.UserIdRequired)
            .GreaterThan(0).WithErrorCode(PollErrors.UserIdRequired);
}

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

/// <summary>Idempotent: adding an existing participant changes nothing, also when two adds race.</summary>
internal sealed class AddParticipantHandler(PollsDbContext db, IValidator<AddParticipantCommand> validator, PollAccess access) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, AddParticipantCommand command, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var participantUserId = command.UserId!.Value;
        if (!await db.Users.AnyAsync(u => u.Id == participantUserId, cancellationToken))
        {
            return Error.NotFound(UserErrors.NotFound);
        }

        if (await db.Participants.AnyAsync(p => p.PollId == pollId && p.UserId == participantUserId, cancellationToken))
        {
            return Result.Success();
        }

        db.Participants.Add(new Participant { PollId = pollId, UserId = participantUserId });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            // added concurrently – same outcome
            db.ChangeTracker.Clear();
        }

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
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // removed concurrently – same outcome
        }

        return Result.Success();
    }
}
