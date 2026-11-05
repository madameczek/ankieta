using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Paging;
using Polls.Api.Common.Results;
using Polls.Api.Common.Validation;
using Polls.Api.Infrastructure;
using Polls.Api.Users.Domain;
using Polls.Api.Users.PublicApi;

namespace Polls.Api.Users.Core;

internal sealed record ListUsersQuery(string? Search, int Page, int PageSize);

internal sealed class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersValidator()
    {
        RuleFor(q => q.Search).MaximumLength(UserLimits.NameMaxLength).WithErrorCode(UserErrors.SearchTooLong);
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithErrorCode(Paging.PageInvalid);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize).WithErrorCode(Paging.PageSizeInvalid);
    }
}

/// <summary>D32: name contains <c>search</c>, sorted by name, paged on the server.</summary>
internal sealed class ListUsersHandler(PollsDbContext db, IValidator<ListUsersQuery> validator) : IHandler
{
    public async Task<Result<PagedResponse<UserListItem>>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(query, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(u => u.Name.Contains(search));
        }

        var total = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderBy(u => u.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserListItem(u.Id, u.Name, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserListItem>(items, query.Page, query.PageSize, total);
    }
}
