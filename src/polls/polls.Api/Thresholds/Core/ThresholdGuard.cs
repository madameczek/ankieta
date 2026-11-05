using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polls.Api.Common.Email;
using Polls.Api.Common.Handlers;
using Polls.Api.Common.Results;
using Polls.Api.Infrastructure;
using Polls.Api.Thresholds.Domain;

namespace Polls.Api.Thresholds.Core;

/// <summary>
/// D14: called before creating a user or poll. Counts all rows (soft-deleted included); returns 503-type error at lockout;
/// at the warning level emails the admin once, recorded in <c>threshold_warnings</c>.
/// </summary>
internal sealed class ThresholdGuard(
    PollsDbContext db,
    IEmailSender emailSender,
    IOptions<ThresholdOptions> options,
    TimeProvider timeProvider,
    ILogger<ThresholdGuard> logger) : IHandler
{
    public const string UsersReachedCode = "limit.users_reached";
    public const string PollsReachedCode = "limit.polls_reached";

    public Task<Result> CheckUsersAsync(CancellationToken cancellationToken) =>
        CheckAsync(ThresholdWarning.Users, db.Users.CountAsync(cancellationToken), options.Value.Users, UsersReachedCode, cancellationToken);

    public Task<Result> CheckPollsAsync(CancellationToken cancellationToken) =>
        CheckAsync(ThresholdWarning.Polls, db.Polls.IgnoreQueryFilters().CountAsync(cancellationToken), options.Value.Polls, PollsReachedCode, cancellationToken);

    private async Task<Result> CheckAsync(string resource, Task<int> countTask, ThresholdLimits limits, string lockedCode, CancellationToken cancellationToken)
    {
        var count = await countTask;

        switch (ThresholdPolicy.Evaluate(count, limits))
        {
            case ThresholdState.Locked:
                logger.LogWarning("Lockout threshold reached for {Resource}: {Count} of {Lockout}", resource, count, limits.Lockout);
                return Error.Unavailable(lockedCode);
            case ThresholdState.Warning:
                await WarnOnceAsync(resource, count, limits, cancellationToken);
                break;
        }

        return Result.Success();
    }

    private async Task WarnOnceAsync(string resource, int count, ThresholdLimits limits, CancellationToken cancellationToken)
    {
        if (await db.ThresholdWarnings.AnyAsync(w => w.Resource == resource, cancellationToken))
        {
            return;
        }

        logger.LogWarning("Warning threshold reached for {Resource}: {Count} of {Warning}", resource, count, limits.Warning);

        var adminEmail = options.Value.AdminEmail;
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            logger.LogWarning("Thresholds:AdminEmail is not set; no warning email sent");
        }
        else
        {
            await emailSender.SendAsync(
                new EmailMessage(
                    adminEmail,
                    $"Polls: {resource} warning threshold reached",
                    $"{resource}: {count + 1} rows after this insert. Warning at {limits.Warning}, creation blocked at {limits.Lockout}."),
                cancellationToken);
        }

        db.ThresholdWarnings.Add(new ThresholdWarning { Resource = resource, SentAt = timeProvider.GetUtcNow().UtcDateTime });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            // Another request recorded it first (race accepted, D14); at worst the email went out twice.
            logger.LogInformation(ex, "Threshold warning for {Resource} already recorded", resource);
            db.ChangeTracker.Clear();
        }
    }
}
