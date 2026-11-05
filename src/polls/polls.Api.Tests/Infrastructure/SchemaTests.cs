using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Time.Testing;
using Polls.Api.Polls.Domain;
using Polls.Api.Users.Domain;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Infrastructure;

/// <summary>Proves the schema (the app's purpose): every constraint from docs/decisions.md is enforced by the database.</summary>
public sealed class SchemaTests(SqlServerFixture database)
{
    private const int UniqueIndexViolation = 2601;
    private const int CheckConstraintViolation = 547;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChanges_SecondOwnerOfPoll_ViolatesUniqueOwnerIndex()
    {
        await using var db = database.CreateContext();
        var poll = await db.AddPollAsync(await db.AddUserAsync(Ct), Ct);
        var other = await db.AddUserAsync(Ct);

        db.Participants.Add(new Participant { PollId = poll.Id, UserId = other.Id, IsOwner = true });
        var act = () => db.SaveChangesAsync(Ct);

        await ShouldFailWith(act, UniqueIndexViolation, "ux_user_poll_owner");
    }

    [Fact]
    public async Task SaveChanges_SeveralNonOwnerParticipants_Succeeds()
    {
        await using var db = database.CreateContext();
        var poll = await db.AddPollAsync(await db.AddUserAsync(Ct), Ct);

        db.Participants.Add(new Participant { PollId = poll.Id, UserId = (await db.AddUserAsync(Ct)).Id });
        db.Participants.Add(new Participant { PollId = poll.Id, UserId = (await db.AddUserAsync(Ct)).Id });
        await db.SaveChangesAsync(Ct);

        (await db.Participants.CountAsync(p => p.PollId == poll.Id, Ct)).Should().Be(3);
    }

    [Fact]
    public async Task SaveChanges_SecondAnswerOfUserToQuestion_ViolatesUniqueIndex()
    {
        await using var db = database.CreateContext();
        var owner = await db.AddUserAsync(Ct);
        var question = (await db.AddPollAsync(owner, Ct, questionCount: 1)).Questions[0];

        db.Answers.Add(new Answer { QuestionId = question.Id, UserId = owner.Id, Text = "first" });
        await db.SaveChangesAsync(Ct);
        db.Answers.Add(new Answer { QuestionId = question.Id, UserId = owner.Id, Text = "second" });
        var act = () => db.SaveChangesAsync(Ct);

        await ShouldFailWith(act, UniqueIndexViolation, "ux_answers_question_user");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveChanges_EmptyAnswer_ViolatesCheck(string text)
    {
        await using var db = database.CreateContext();
        var owner = await db.AddUserAsync(Ct);
        var question = (await db.AddPollAsync(owner, Ct, questionCount: 1)).Questions[0];

        db.Answers.Add(new Answer { QuestionId = question.Id, UserId = owner.Id, Text = text });
        var act = () => db.SaveChangesAsync(Ct);

        await ShouldFailWith(act, CheckConstraintViolation, "ck_answers_answer_not_empty");
    }

    [Fact]
    public async Task SaveChanges_DuplicateSortOrderAmongActiveQuestions_ViolatesUniqueIndex()
    {
        await using var db = database.CreateContext();
        var poll = await db.AddPollAsync(await db.AddUserAsync(Ct), Ct, questionCount: 1);

        db.Questions.Add(new Question { PollId = poll.Id, SortOrder = 1, Text = "duplicate order" });
        var act = () => db.SaveChangesAsync(Ct);

        await ShouldFailWith(act, UniqueIndexViolation, "ux_questions_poll_sort_order");
    }

    [Fact]
    public async Task SaveChanges_SortOrderOfSoftDeletedQuestionReused_Succeeds()
    {
        await using var db = database.CreateContext();
        var poll = await db.AddPollAsync(await db.AddUserAsync(Ct), Ct, questionCount: 1);

        poll.Questions[0].DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(Ct);
        db.Questions.Add(new Question { PollId = poll.Id, SortOrder = 1, Text = "reuses order 1" });
        await db.SaveChangesAsync(Ct);

        (await db.Questions.SingleAsync(q => q.PollId == poll.Id, Ct)).Text.Should().Be("reuses order 1");
    }

    [Fact]
    public async Task SaveChanges_DuplicateUserName_ViolatesUniqueIndex()
    {
        await using var db = database.CreateContext();
        var existing = await db.AddUserAsync(Ct);

        db.Users.Add(new User { Name = existing.Name });
        var act = () => db.SaveChangesAsync(Ct);

        await ShouldFailWith(act, UniqueIndexViolation, "ux_users_name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task SaveChanges_EmptyUserName_ViolatesCheck(string name)
    {
        await using var db = database.CreateContext();

        db.Users.Add(new User { Name = name });
        var act = () => db.SaveChangesAsync(Ct);

        await ShouldFailWith(act, CheckConstraintViolation, "ck_users_name_not_empty");
    }

    [Fact]
    public async Task Query_SoftDeletedPoll_HidesPollAndEverythingUnderIt()
    {
        int pollId;
        await using (var setup = database.CreateContext())
        {
            var owner = await setup.AddUserAsync(Ct);
            var poll = await setup.AddPollAsync(owner, Ct, questionCount: 2);
            setup.Answers.Add(new Answer { QuestionId = poll.Questions[0].Id, UserId = owner.Id, Text = "an answer" });
            poll.DeletedAt = DateTime.UtcNow;
            await setup.SaveChangesAsync(Ct);
            pollId = poll.Id;
        }

        await using var db = database.CreateContext();

        (await db.Polls.AnyAsync(p => p.Id == pollId, Ct)).Should().BeFalse();
        (await db.Questions.AnyAsync(q => q.PollId == pollId, Ct)).Should().BeFalse();
        (await db.Participants.AnyAsync(p => p.PollId == pollId, Ct)).Should().BeFalse();
        (await db.Answers.AnyAsync(a => a.Question.PollId == pollId, Ct)).Should().BeFalse();
        (await db.Polls.IgnoreQueryFilters().AnyAsync(p => p.Id == pollId, Ct)).Should().BeTrue("soft delete keeps the row");
    }

    [Fact]
    public async Task SaveChanges_QuestionEditedWithoutLoadingPoll_BumpsPollModifiedAt()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        int pollId, questionId;
        await using (var setup = database.CreateContext(time))
        {
            var poll = await setup.AddPollAsync(await setup.AddUserAsync(Ct), Ct, questionCount: 1);
            (pollId, questionId) = (poll.Id, poll.Questions[0].Id);
        }

        time.Advance(TimeSpan.FromHours(1));
        await using (var db = database.CreateContext(time))
        {
            var question = await db.Questions.SingleAsync(q => q.Id == questionId, Ct);
            question.Text = "edited";
            await db.SaveChangesAsync(Ct);
        }

        await using var check = database.CreateContext();
        var reloaded = await check.Polls.SingleAsync(p => p.Id == pollId, Ct);
        reloaded.CreatedAt.Should().Be(new DateTime(2026, 1, 1, 10, 0, 0));
        reloaded.ModifiedAt.Should().Be(new DateTime(2026, 1, 1, 11, 0, 0));
        reloaded.Title.Should().StartWith("poll-", "the stub update must not overwrite other columns");
    }

    [Fact]
    public async Task SaveChanges_AnswerAdded_DoesNotBumpPollModifiedAt()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        await using var db = database.CreateContext(time);
        var owner = await db.AddUserAsync(Ct);
        var poll = await db.AddPollAsync(owner, Ct, questionCount: 1);

        time.Advance(TimeSpan.FromHours(1));
        db.Answers.Add(new Answer { QuestionId = poll.Questions[0].Id, UserId = owner.Id, Text = "answer" });
        await db.SaveChangesAsync(Ct);

        await using var check = database.CreateContext();
        var reloaded = await check.Polls.SingleAsync(p => p.Id == poll.Id, Ct);
        reloaded.ModifiedAt.Should().Be(new DateTime(2026, 1, 1, 10, 0, 0));
        (await check.Answers.SingleAsync(a => a.QuestionId == poll.Questions[0].Id, Ct))
            .ModifiedAt.Should().Be(new DateTime(2026, 1, 1, 11, 0, 0));
    }

    [Fact]
    public async Task Model_ComparedToMigrations_HasNoPendingChanges()
    {
        await using var db = database.CreateContext();

        db.Database.HasPendingModelChanges().Should().BeFalse("run `dotnet ef migrations add` after model changes");
    }

    [Fact]
    public async Task MigrationScript_Committed_MatchesMigrations()
    {
        await using var db = database.CreateContext();

        var generated = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

        Normalize(SqlServerFixture.ReadScript()).Should().Be(
            Normalize(generated),
            "regenerate db/migrations.sql with `dotnet ef migrations script --idempotent`");
    }

    private static string Normalize(string sql) => sql.ReplaceLineEndings("\n").Trim();

    private static async Task ShouldFailWith(Func<Task> act, int sqlErrorNumber, string constraintName)
    {
        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        var sql = thrown.Which.InnerException.Should().BeOfType<SqlException>().Subject;
        sql.Number.Should().Be(sqlErrorNumber);
        sql.Message.Should().Contain(constraintName);
    }
}
