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
    private const string AdminEmail = "admin@example.test";

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
        await _email.Received(1).SendAsync(Arg.Is<EmailMessage>(m => m.To == AdminEmail && m.Subject.Contains("users")), Arg.Any<CancellationToken>());
        (await db.ThresholdWarnings.SingleAsync(Ct)).Resource.Should().Be(ThresholdWarning.Users);
    }

    [Fact]
    public async Task CheckUsers_WarningWithoutAdminEmail_RecordsWithoutEmail()
    {
        await using var db = await CleanContextAsync();

        var result = await Guard(db, users: AlwaysWarning, adminEmail: null).CheckUsersAsync(Ct);

        result.IsSuccess.Should().BeTrue();
        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, Ct);
        (await db.ThresholdWarnings.AnyAsync(w => w.Resource == ThresholdWarning.Users, Ct)).Should().BeTrue();
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

    private async Task<PollsDbContext> CleanContextAsync()
    {
        var db = database.CreateContext();
        await db.ThresholdWarnings.ExecuteDeleteAsync(Ct);
        return db;
    }

    private ThresholdGuard Guard(PollsDbContext db, ThresholdLimits? users = null, ThresholdLimits? polls = null, string? adminEmail = AdminEmail) =>
        new(
            db,
            _email,
            Options.Create(new ThresholdOptions
            {
                AdminEmail = adminEmail,
                Users = users ?? new ThresholdLimits(int.MaxValue, int.MaxValue),
                Polls = polls ?? new ThresholdLimits(int.MaxValue, int.MaxValue),
            }),
            TimeProvider.System,
            NullLogger<ThresholdGuard>.Instance);
}
