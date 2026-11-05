using System.Security.Claims;
using Polls.Api.Common;
using Polls.Api.Common.Endpoints;
using Polls.Api.Common.Http;
using Polls.Api.Users.Core;
using Polls.Api.Users.Infrastructure;
using Polls.Api.Users.PublicApi;

namespace Polls.Api.Users.Endpoints;

/// <summary>D10: current user, "become user", "leave". Open to visitors.</summary>
internal sealed class SessionEndpoints : IApiEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RouteConsts.Session).WithTags("Session");

        group.MapGet("", GetAsync);
        group.MapPost("{userId:int}", BecomeAsync);
        // (Delegate): LeaveAsync(HttpContext) would otherwise bind as a RequestDelegate and drop the result
        group.MapDelete("", (Delegate)LeaveAsync);
    }

    private static async Task<IResult> GetAsync(ClaimsPrincipal principal, GetUserHandler handler, CancellationToken cancellationToken)
    {
        if (principal.CurrentUserId() is not { } userId)
        {
            return TypedResults.Ok(new SessionResponse(null));
        }

        var user = await handler.HandleAsync(userId, cancellationToken);
        return TypedResults.Ok(new SessionResponse(user.IsSuccess ? user.Value : null));
    }

    private static async Task<IResult> BecomeAsync(int userId, GetUserHandler handler, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        await http.SignInUserAsync(result.Value.Id, result.Value.Name);
        return TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> LeaveAsync(HttpContext http)
    {
        await http.SignOutUserAsync();
        return TypedResults.NoContent();
    }
}
