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
    IOptions<EmailApiOptions> emailOptions,
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

        // Invalid EmailApi settings select the logging stub at startup (D17), so no address checks here.
        try
        {
            await emailSender.SendAsync(WarningEmail(emailOptions.Value.To, resource, count + 1, limits), cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException { InnerException: TimeoutException })
        {
            // The create must not fail because of the mail API; the warning is still recorded, so it is not retried on every insert.
            logger.LogError(ex, "Threshold warning email for {Resource} not sent", resource);
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

    /// <summary>The admin's warning email, in Polish like the UI (D28); the sender adds the "[tag] " subject prefix. <paramref name="total"/> includes the new row.</summary>
    internal static EmailMessage WarningEmail(string to, string resource, int total, ThresholdLimits limits)
    {
        var users = resource == ThresholdWarning.Users;
        var what = users ? "użytkowników" : "ankiet";
        var counted = users ? "Liczba użytkowników" : "Liczba ankiet (razem z usuniętymi)";

        return new EmailMessage(
            to,
            $"Liczba {what} osiągnęła próg ostrzeżenia",
            $"{counted} po ostatnim zapisie: {total}. Próg ostrzeżenia: {limits.Warning}. " +
            $"Przy {limits.Lockout} tworzenie nowych {what} zostanie zablokowane.");
    }
}
