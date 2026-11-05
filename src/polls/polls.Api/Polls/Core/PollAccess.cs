using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Infrastructure;

namespace Polls.Api.Polls.Core;

/// <summary>
/// D41: a user who isn't a participant gets 404 for the poll and everything under it (deleted polls likewise, via the
/// query filter); a participant calling an owner action gets 403.
/// </summary>
internal sealed class PollAccess(PollsDbContext db) : IHandler
{
    public async Task<Result> RequireParticipantAsync(int pollId, int userId, CancellationToken cancellationToken) =>
        await IsOwnerAsync(pollId, userId, cancellationToken) is null
            ? Error.NotFound(PollErrors.NotFound)
            : Result.Success();

    public async Task<Result> RequireOwnerAsync(int pollId, int userId, CancellationToken cancellationToken) =>
        await IsOwnerAsync(pollId, userId, cancellationToken) switch
        {
            null => Error.NotFound(PollErrors.NotFound),
            false => Error.Forbidden(PollErrors.OwnerRequired),
            true => Result.Success(),
        };

    /// <summary>null = not a participant (or the poll is deleted).</summary>
    public Task<bool?> IsOwnerAsync(int pollId, int userId, CancellationToken cancellationToken) =>
        db.Participants
            .Where(p => p.PollId == pollId && p.UserId == userId)
            .Select(p => (bool?)p.IsOwner)
            .SingleOrDefaultAsync(cancellationToken);
}
