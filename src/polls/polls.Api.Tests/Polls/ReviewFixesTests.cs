using System.Net;
using System.Net.Http.Json;
using System.Text;
using AwesomeAssertions;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Polls;

/// <summary>Behaviour added after the backend review: error codes everywhere, userId validation, timestamps, create limits.</summary>
public sealed class ReviewFixesTests(SqlServerFixture database) : PollTestBase(database)
{
    [Fact]
    public async Task Post_MalformedJson_Returns400RequestInvalid()
    {
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        using var response = await Owner.PostAsync("/api/polls", content, Ct);

        await ShouldBeProblem(response, HttpStatusCode.BadRequest, "request.invalid");
    }

    [Fact]
    public async Task Post_MissingBody_Returns400RequestInvalid()
    {
        using var response = await Owner.PostAsync($"/api/polls/{PollId}/questions", null, Ct);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
        (await response.ReadProblemCodeAsync(Ct)).Should().Be("request.invalid");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"userId\":0}")]
    public async Task PostParticipant_MissingOrZeroUserId_Returns400UserIdRequired(string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await Owner.PostAsync($"/api/polls/{PollId}/participants", content, Ct);

        await ShouldBeValidationError(response, "userId", "userId.required");
    }

    [Fact]
    public async Task PutAnswer_SameTextAgain_UpdatesModifiedAt()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);
        var first = await SaveAsync(question, "same");
        await Task.Delay(20, Ct);

        var second = await SaveAsync(question, "same");

        second.Should().BeAfter(first);
    }

    [Fact]
    public async Task PollModifiedAt_ChangesOnReorderButNotOnAnswer()
    {
        var a = await Owner.AddQuestionAsync(PollId, Ct);
        var b = await Owner.AddQuestionAsync(PollId, Ct);
        var afterQuestions = await PollModifiedAtAsync();

        await Task.Delay(20, Ct);
        await Participant.SaveAnswerAsync(PollId, a, "answer", Ct);
        var afterAnswer = await PollModifiedAtAsync();

        (await Owner.PutAsJsonAsync($"/api/polls/{PollId}/questions/order", new { questionIds = new[] { b, a } }, Ct)).Dispose();
        var afterReorder = await PollModifiedAtAsync();

        afterAnswer.Should().Be(afterQuestions);
        afterReorder.Should().BeAfter(afterAnswer);
    }

    [Fact]
    public async Task PostQuestion_CreateRateLimitExceeded_Returns429()
    {
        // user + poll use the two permits of the shared "create" policy; the question is the third create (D15)
        await using var limited = new PollsApiFactory(Database.ConnectionString, new Dictionary<string, string?> { ["RateLimiting:Create:PermitLimit"] = "2" });
        using var client = limited.CreateClient();
        await client.CreateUserAsync(Ct);
        var pollId = await client.CreatePollAsync(Ct);

        using var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/questions", new { question = "q" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.TooManyRequests, "rate_limited");
    }

    private async Task<DateTime> SaveAsync(int questionId, string answer)
    {
        using var response = await Participant.PutAsJsonAsync($"/api/polls/{PollId}/questions/{questionId}/answer", new { answer }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SavedAnswerDto>(Ct))!.ModifiedAt;
    }

    private async Task<DateTime> PollModifiedAtAsync() =>
        (await Owner.GetMyPollsAsync(Ct)).Single(p => p.Id == PollId).ModifiedAt;

    private sealed record SavedAnswerDto(DateTime ModifiedAt);
}
