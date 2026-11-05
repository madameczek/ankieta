using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Common.Validation;
using Polls.Api.Infrastructure;
using Polls.Api.Polls.Domain;
using Polls.Api.Polls.PublicApi;

namespace Polls.Api.Polls.Core;

internal sealed record AnswerCommand(string? Answer);

internal sealed class AnswerCommandValidator : AbstractValidator<AnswerCommand>
{
    public AnswerCommandValidator() =>
        RuleFor(c => c.Answer)
            .Must(a => a is null || a.Length <= PollLimits.AnswerMaxLength).WithErrorCode(PollErrors.AnswerTooLong);
}

/// <summary>
/// D29/D31: upserts the caller's answer to one question; empty or whitespace-only text deletes it.
/// Returns the new modified time, or null when the answer was cleared.
/// </summary>
internal sealed class SaveAnswerHandler(PollsDbContext db, IValidator<AnswerCommand> validator, PollAccess access) : IHandler
{
    public async Task<Result<SavedAnswerResponse?>> HandleAsync(int userId, int pollId, int questionId, AnswerCommand command, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireParticipantAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed.Error;
        }

        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (!await db.Questions.AnyAsync(q => q.Id == questionId && q.PollId == pollId, cancellationToken))
        {
            return Error.NotFound(PollErrors.QuestionNotFound);
        }

        var answer = await db.Answers.SingleOrDefaultAsync(a => a.QuestionId == questionId && a.UserId == userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(command.Answer))
        {
            if (answer is not null)
            {
                db.Answers.Remove(answer);
                await db.SaveChangesAsync(cancellationToken);
            }

            return Result<SavedAnswerResponse?>.Success(null);
        }

        if (answer is null)
        {
            answer = new Answer { QuestionId = questionId, UserId = userId, Text = command.Answer };
            db.Answers.Add(answer);
        }
        else
        {
            answer.Text = command.Answer;
            // Saving the same text again still counts as an edit (D29): the interceptor sets the new time.
            db.Entry(answer).Property(a => a.ModifiedAt).IsModified = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new SavedAnswerResponse(answer.ModifiedAt);
    }
}

/// <summary>D33: answers per question for the owner; only current participants' answers (D25).</summary>
internal sealed class GetResultsHandler(PollsDbContext db, PollAccess access) : IHandler
{
    public async Task<Result<PollResultsResponse>> HandleAsync(int userId, int pollId, CancellationToken cancellationToken)
    {
        var allowed = await access.RequireOwnerAsync(pollId, userId, cancellationToken);
        if (!allowed.IsSuccess)
        {
            return allowed.Error;
        }

        var participantIds = db.Participants.Where(p => p.PollId == pollId).Select(p => p.UserId);

        var questions = await db.Questions.AsNoTracking()
            .Where(q => q.PollId == pollId)
            .OrderBy(q => q.SortOrder)
            .Select(q => new ResultQuestion(
                q.Id,
                q.SortOrder,
                q.Text,
                q.Answers
                    .Where(a => participantIds.Contains(a.UserId))
                    .OrderBy(a => a.User.Name)
                    .Select(a => new ResultAnswer(a.UserId, a.User.Name, a.Text, a.ModifiedAt))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PollResultsResponse(await participantIds.CountAsync(cancellationToken), questions);
    }
}
