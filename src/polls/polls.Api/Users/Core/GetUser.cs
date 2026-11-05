using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Infrastructure;
using Polls.Api.Users.PublicApi;

namespace Polls.Api.Users.Core;

/// <summary>Loads a user for "become user" and for the current session.</summary>
internal sealed class GetUserHandler(PollsDbContext db) : IHandler
{
    public async Task<Result<UserResponse>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserResponse(u.Id, u.Name))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? Error.NotFound(UserErrors.NotFound) : user;
    }
}
