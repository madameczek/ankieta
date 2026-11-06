using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Polls.Api.Common.Email;
using Polls.Api.Common.Results;
using Polls.Api.Infrastructure;
using Polls.Api.Thresholds.Core;
using Polls.Api.Thresholds.Domain;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Thresholds;

/// <summary>
/// Limits are set relative to "any count" (0 / int.MaxValue), so rows created by parallel tests don't matter.
/// Only this class writes threshold_warnings; it clears the table per test (tests in one class run sequentially).
/// </summary>
public sealed class ThresholdGuardTests(SqlServerFixture database)
{
    private const string To = "admin@example.test";

    private static readonly ThresholdLimits AlwaysLocked = new(Warning: 0, Lockout: 0);
    private static readonly ThresholdLimits AlwaysWarning = new(Warning: 0, Lockout: int.MaxValue);

    private readonly IEmailSender _email = Substitute.For<IEmailSender>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CheckUsers_LockoutReached_ReturnsUsersReachedWithoutEmail()
    {
        await using var db = await CleanContextAsync();

        var result = await Guard(db, users: AlwaysLocked).CheckUsersAsync(Ct);

        result.Error.Should().Be(Error.Unavailable("limit.users_reached"));
        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, Ct);
    }

    [Fact]
    public async Task CheckPolls_LockoutReached_ReturnsPollsReached()
    {
        await using var db = await CleanContextAsync();

        var result = await Guard(db, polls: AlwaysLocked).CheckPollsAsync(Ct);

        result.Error.Should().Be(Error.Unavailable("limit.polls_reached"));
    }

    [Fact]
    public async Task CheckPolls_SoftDeletedPollsCount_TowardLockout()
    {
        await using var db = await CleanContextAsync();
        var poll = await db.AddPollAsync(await db.AddUserAsync(Ct), Ct);
        poll.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(Ct);
        var allRows = await db.Polls.IgnoreQueryFilters().CountAsync(Ct);

        var result = await Guard(db, polls: new ThresholdLimits(Warning: int.MaxValue, Lockout: allRows)).CheckPollsAsync(Ct);

        result.Error.Should().Be(Error.Unavailable("limit.polls_reached"));
    }

    [Fact]
    public async Task CheckUsers_WarningFirstTime_EmailsAdminOnceAndRecordsIt()
    {
        await using var db = await CleanContextAsync();
        var guard = Guard(db, users: AlwaysWarning);

        var first = await guard.CheckUsersAsync(Ct);
        var second = await guard.CheckUsersAsync(Ct);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        await _email.Received(1).SendAsync(Arg.Is<EmailMessage>(m => m.To == To && m.Subject.Contains("użytkowników")), Arg.Any<CancellationToken>());
        (await db.ThresholdWarnings.SingleAsync(Ct)).Resource.Should().Be(ThresholdWarning.Users);
    }

    [Fact]
    public async Task CheckUsers_WarningEmailFails_SucceedsAndRecordsWarning()
    {
        await using var db = await CleanContextAsync();
        _email.SendAsync(default!, Ct).ReturnsForAnyArgs(Task.FromException(new HttpRequestException("mail API down")));

        var result = await Guard(db, users: AlwaysWarning).CheckUsersAsync(Ct);

        result.IsSuccess.Should().BeTrue();
        (await db.ThresholdWarnings.SingleAsync(Ct)).Resource.Should().Be(ThresholdWarning.Users);
    }

    [Fact]
    public async Task CheckUsers_WarningEmailTimesOut_SucceedsAndRecordsWarning()
    {
        await using var db = await CleanContextAsync();
        _email.SendAsync(default!, Ct).ReturnsForAnyArgs(Task.FromException(new TaskCanceledException("timeout", new TimeoutException())));

        var result = await Guard(db, users: AlwaysWarning).CheckUsersAsync(Ct);

        result.IsSuccess.Should().BeTrue();
        (await db.ThresholdWarnings.SingleAsync(Ct)).Resource.Should().Be(ThresholdWarning.Users);
    }

    [Fact]
    public async Task CheckUsers_WarningEmailCanceled_ThrowsAndRecordsNothing()
    {
        await using var db = await CleanContextAsync();
        _email.SendAsync(default!, Ct).ReturnsForAnyArgs(Task.FromException(new TaskCanceledException()));

        var check = () => Guard(db, users: AlwaysWarning).CheckUsersAsync(Ct);

        await check.Should().ThrowAsync<TaskCanceledException>();
        (await db.ThresholdWarnings.AnyAsync(Ct)).Should().BeFalse();
    }

    [Fact]
    public async Task CheckUsers_BelowWarning_DoesNothing()
    {
        await using var db = await CleanContextAsync();

        var result = await Guard(db, users: new ThresholdLimits(int.MaxValue, int.MaxValue)).CheckUsersAsync(Ct);

        result.IsSuccess.Should().BeTrue();
        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, Ct);
        (await db.ThresholdWarnings.AnyAsync(Ct)).Should().BeFalse();
    }

    [Fact]
    public void WarningEmail_Users_PolishSubjectAndBodyWithCountAndLimits()
    {
        var email = ThresholdGuard.WarningEmail(To, ThresholdWarning.Users, 2000, new ThresholdLimits(Warning: 2000, Lockout: 3000));

        email.To.Should().Be(To);
        email.Subject.Should().Be("Liczba użytkowników osiągnęła próg ostrzeżenia");
        email.Body.Should().Be(
            "Liczba użytkowników po ostatnim zapisie: 2000. Próg ostrzeżenia: 2000. Przy 3000 tworzenie nowych użytkowników zostanie zablokowane.");
    }

    [Fact]
    public void WarningEmail_Polls_SaysDeletedPollsCount()
    {
        var email = ThresholdGuard.WarningEmail(To, ThresholdWarning.Polls, 3001, new ThresholdLimits(Warning: 3000, Lockout: 5000));

        email.Subject.Should().Be("Liczba ankiet osiągnęła próg ostrzeżenia");
        email.Body.Should().StartWith("Liczba ankiet (razem z usuniętymi) po ostatnim zapisie: 3001.");
        email.Body.Should().EndWith("Przy 5000 tworzenie nowych ankiet zostanie zablokowane.");
    }

    private async Task<PollsDbContext> CleanContextAsync()
    {
        var db = database.CreateContext();
        await db.ThresholdWarnings.ExecuteDeleteAsync(Ct);
        return db;
    }

    private ThresholdGuard Guard(PollsDbContext db, ThresholdLimits? users = null, ThresholdLimits? polls = null) =>
        new(
            db,
            _email,
            Options.Create(new ThresholdOptions
            {
                Users = users ?? new ThresholdLimits(int.MaxValue, int.MaxValue),
                Polls = polls ?? new ThresholdLimits(int.MaxValue, int.MaxValue),
            }),
            Options.Create(new EmailApiOptions { To = To }),
            TimeProvider.System,
            NullLogger<ThresholdGuard>.Instance);
}
