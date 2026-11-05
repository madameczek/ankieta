using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Common.Validation;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;
using Polls.Api.Polls.PublicApi;
using Polls.Api.Thresholds.Core;

namespace Polls.Api.Polls.Core;

internal sealed record PollCommand(string? Title, string? Description);

internal sealed class PollCommandValidator : AbstractValidator<PollCommand>
{
    public PollCommandValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty().WithErrorCode(PollErrors.TitleRequired)
            .Must(t => t!.Trim().Length <= PollLimits.TitleMaxLength).WithErrorCode(PollErrors.TitleTooLong);
        RuleFor(c => c.Description)
            .Must(d => d is null || d.Trim().Length <= PollLimits.DescriptionMaxLength).WithErrorCode(PollErrors.DescriptionTooLong);
    }
}

internal static class PollCommandExtensions
{
    public static string NormalizedTitle(this PollCommand command) => command.Title!.Trim();

    public static string? NormalizedDescription(this PollCommand command) =>
        string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
}

/// <summary>D35: owned and participating polls together, newest change first.</summary>
internal sealed class ListMyPollsHandler(PollsDbContext db) : IHandler
{
    public async Task<IReadOnlyList<PollListItem>> HandleAsync(int userId, CancellationToken cancellationToken) =>
        await db.Participants.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Poll.ModifiedAt)
            .Select(p => new PollListItem(
                p.PollId,
                p.Poll.Title,
                p.IsOwner ? PollRoles.Owner : PollRoles.Participant,
                db.Answers.Count(a => a.UserId == userId && a.Question.PollId == p.PollId),
                db.Questions.Count(q => q.PollId == p.PollId),
                p.Poll.ModifiedAt))
            .ToListAsync(cancellationToken);
}

/// <summary>The creator becomes the owner, in the same transaction (D4); threshold check first (D14).</summary>
internal sealed class CreatePollHandler(PollsDbContext db, IValidator<PollCommand> validator, ThresholdGuard thresholds) : IHandler
{
    public async Task<Result<CreatedPollResponse>> HandleAsync(int userId, PollCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var limit = await thresholds.CheckPollsAsync(cancellationToken);
        if (!limit.IsSuccess)
        {
            return limit.Error;
        }

        var poll = new Poll { Title = command.NormalizedTitle(), Description = command.NormalizedDescription() };
        poll.Participants.Add(new Participant { UserId = userId, IsOwner = true });
        db.Polls.Add(poll);
        await db.SaveChangesAsync(cancellationToken);

        return new CreatedPollResponse(poll.Id);
    }
}

/// <summary>Participant view and owner editor share this (questions in order, with the caller's own answers).</summary>
internal sealed class GetPollHandler(PollsDbContext db, PollAccess access) : IHandler
{
    public async Task<Result<PollDetailResponse>> HandleAsync(int userId, int pollId, CancellationToken cancellationToken)
    {
        if (await access.IsOwnerAsync(pollId, userId, cancellationToken) is not { } isOwner)
        {
            return Error.NotFound(PollErrors.NotFound);
        }

        var poll = await db.Polls.AsNoTracking()
            .Where(p => p.Id == pollId)
            .Select(p => new { p.Title, p.Description })
            .SingleAsync(cancellationToken);

        var questions = await db.Questions.AsNoTracking()
            .Where(q => q.PollId == pollId)
            .OrderBy(q => q.SortOrder)
            .Select(q => new PollQuestionView(
                q.Id,
                q.SortOrder,
                q.Text,
                q.Answers.Where(a => a.UserId == userId).Select(a => a.Text).FirstOrDefault(),
                q.Answers.Where(a => a.UserId == userId).Select(a => (DateTime?)a.ModifiedAt).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PollDetailResponse(pollId, poll.Title, poll.Description, PollRoles.Of(isOwner), questions);
    }
}

internal sealed class UpdatePollHandler(PollsDbContext db, IValidator<PollCommand> validator, PollAccess access) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, PollCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var poll = await db.Polls.SingleAsync(p => p.Id == pollId, cancellationToken);
        poll.Title = command.NormalizedTitle();
        poll.Description = command.NormalizedDescription();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>Soft delete (D8); everything under the poll disappears through the query filters.</summary>
internal sealed class DeletePollHandler(PollsDbContext db, PollAccess access, TimeProvider timeProvider) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var poll = await db.Polls.SingleAsync(p => p.Id == pollId, cancellationToken);
        poll.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
