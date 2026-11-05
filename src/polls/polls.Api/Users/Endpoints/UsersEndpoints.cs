using Polls.Api.Common;
using Polls.Api.Common.Endpoints;
using Polls.Api.Common.Http;
using Polls.Api.Common.Paging;
using Polls.Api.Common.RateLimiting;
using Polls.Api.Users.Core;
using Polls.Api.Users.Infrastructure;
using Polls.Api.Users.PublicApi;

namespace Polls.Api.Users.Endpoints;

/// <summary>User list and creation. Open to visitors (D11).</summary>
internal sealed class UsersEndpoints : IApiEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RouteConsts.Users).WithTags("Users");

        group.MapGet("", ListAsync);
        group.MapPost("", CreateAsync).RequireRateLimiting(RateLimitPolicies.Create);
    }

    private static async Task<IResult> ListAsync(
        ListUsersHandler handler,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1,
        int pageSize = Paging.DefaultPageSize)
    {
        var result = await handler.HandleAsync(new ListUsersQuery(search, page, pageSize), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    /// <summary>Creates the user and enters their context (D10).</summary>
    private static async Task<IResult> CreateAsync(CreateUserRequest request, CreateUserHandler handler, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateUserCommand(request.Name), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        await http.SignInUserAsync(result.Value.Id, result.Value.Name);
        return TypedResults.Created($"{RouteConsts.Users}/{result.Value.Id}", result.Value);
    }
}
