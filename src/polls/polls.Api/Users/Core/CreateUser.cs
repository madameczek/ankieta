using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Common.Validation;
using Polls.Api.Infrastructure;
using Polls.Api.Thresholds.Core;
using Polls.Api.Users.Domain;
using Polls.Api.Users.PublicApi;

namespace Polls.Api.Users.Core;

internal sealed record CreateUserCommand(string? Name);

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator() =>
        RuleFor(c => c.Name)
            .NotEmpty().WithErrorCode(UserErrors.NameRequired)
            .Must(n => n!.Trim().Length <= UserLimits.NameMaxLength).WithErrorCode(UserErrors.NameTooLong);
}

internal sealed class CreateUserHandler(
    PollsDbContext db,
    IValidator<CreateUserCommand> validator,
    ThresholdGuard thresholds) : IHandler
{
    private const int UniqueIndexViolation = 2601;

    public async Task<Result<UserResponse>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var name = command.Name!.Trim();

        // Name comparison follows the column collation (case-insensitive by default), same as the unique index (D6).
        if (await db.Users.AnyAsync(u => u.Name == name, cancellationToken))
        {
            return Error.Conflict(UserErrors.NameTaken);
        }

        var limit = await thresholds.CheckUsersAsync(cancellationToken);
        if (!limit.IsSuccess)
        {
            return limit.Error;
        }

        var user = new User { Name = name };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation })
        {
            // Same name created concurrently, after the check above.
            return Error.Conflict(UserErrors.NameTaken);
        }

        return new UserResponse(user.Id, user.Name);
    }
}
