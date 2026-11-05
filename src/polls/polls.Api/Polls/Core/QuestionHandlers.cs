using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Common.Validation;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;
using Polls.Api.Polls.PublicApi;

namespace Polls.Api.Polls.Core;

internal sealed record QuestionCommand(string? Question);

internal sealed class QuestionCommandValidator : AbstractValidator<QuestionCommand>
{
    public QuestionCommandValidator() =>
        RuleFor(c => c.Question)
            .NotEmpty().WithErrorCode(PollErrors.QuestionRequired)
            .Must(q => q!.Trim().Length <= PollLimits.QuestionMaxLength).WithErrorCode(PollErrors.QuestionTooLong);
}

internal sealed record QuestionOrderCommand(IReadOnlyList<int>? QuestionIds);

internal sealed class QuestionOrderCommandValidator : AbstractValidator<QuestionOrderCommand>
{
    public QuestionOrderCommandValidator() =>
        RuleFor(c => c.QuestionIds).NotNull().WithErrorCode(PollErrors.QuestionIdsRequired);
}

/// <summary>Appends the question at the end (max + 1). Owner only (D26).</summary>
internal sealed class AddQuestionHandler(PollsDbContext db, IValidator<QuestionCommand> validator, PollAccess access) : IHandler
{
    private const int Attempts = 3;

    public async Task<Result<CreatedQuestionResponse>> HandleAsync(int userId, int pollId, QuestionCommand command, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed.Error;
        }

        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        // A concurrent add can take the same max + 1; the unique (poll, sort_order) index rejects it, so read the max again.
        for (var attempt = 1; ; attempt++)
        {
            var last = await db.Questions.Where(q => q.PollId == pollId).MaxAsync(q => (int?)q.SortOrder, cancellationToken) ?? 0;
            var question = new Question { PollId = pollId, SortOrder = last + 1, Text = command.Question!.Trim() };
            db.Questions.Add(question);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return new CreatedQuestionResponse(question.Id, question.SortOrder);
            }
            catch (DbUpdateException ex) when (ex.IsDuplicateKey() && attempt < Attempts)
            {
                db.ChangeTracker.Clear();
            }
        }
    }
}

/// <summary>Edits the text; answers are kept (D8).</summary>
internal sealed class UpdateQuestionHandler(PollsDbContext db, IValidator<QuestionCommand> validator, PollAccess access) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, int questionId, QuestionCommand command, CancellationToken cancellationToken)
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

        var question = await db.Questions.SingleOrDefaultAsync(q => q.Id == questionId && q.PollId == pollId, cancellationToken);
        if (question is null)
        {
            return Error.NotFound(PollErrors.QuestionNotFound);
        }

        question.Text = command.Question!.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>Soft delete (D8); gaps in sort_order are allowed.</summary>
internal sealed class DeleteQuestionHandler(PollsDbContext db, PollAccess access, TimeProvider timeProvider) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, int questionId, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var question = await db.Questions.SingleOrDefaultAsync(q => q.Id == questionId && q.PollId == pollId, cancellationToken);
        if (question is null)
        {
            return Error.NotFound(PollErrors.QuestionNotFound);
        }

        question.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// D9: the list must contain exactly the poll's non-deleted questions; they are renumbered 1..n in one serializable
/// transaction (reading the questions included, so no add can slip in between). Two steps (first to -1..-n, then to 1..n)
/// so the unique (poll, sort_order) index never sees a temporary duplicate.
/// </summary>
internal sealed class ReorderQuestionsHandler(PollsDbContext db, IValidator<QuestionOrderCommand> validator, PollAccess access) : IHandler
{
    public async Task<Result> HandleAsync(int userId, int pollId, QuestionOrderCommand command, CancellationToken cancellationToken)
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

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var questions = await db.Questions.Where(q => q.PollId == pollId).ToDictionaryAsync(q => q.Id, cancellationToken);
        var order = command.QuestionIds!;
        if (order.Count != questions.Count || order.Distinct().Count() != order.Count || !order.All(questions.ContainsKey))
        {
            return Error.Conflict(PollErrors.OrderMismatch);
        }

        for (var i = 0; i < order.Count; i++)
        {
            questions[order[i]].SortOrder = -(i + 1);
        }

        await db.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < order.Count; i++)
        {
            questions[order[i]].SortOrder = i + 1;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
