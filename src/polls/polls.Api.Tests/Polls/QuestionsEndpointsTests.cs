using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Polls;

public sealed class QuestionsEndpointsTests(SqlServerFixture database) : PollTestBase(database)
{
    [Fact]
    public async Task Post_Questions_AppendedInOrder()
    {
        await Owner.AddQuestionAsync(PollId, Ct, "first");
        using var response = await Owner.PostAsJsonAsync($"/api/polls/{PollId}/questions", new { question = "  second  " }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Owner.GetPollAsync(PollId, Ct)).Questions.Select(q => (q.SortOrder, q.Question))
            .Should().Equal((1, "first"), (2, "second"));
    }

    [Theory]
    [InlineData(null, "question.required")]
    [InlineData(" ", "question.required")]
    [InlineData(1001, "question.too_long")]
    public async Task Post_Invalid_Returns400(object? question, string code)
    {
        var text = question is int length ? new string('x', length) : (string?)question;

        using var response = await Owner.PostAsJsonAsync($"/api/polls/{PollId}/questions", new { question = text }, Ct);

        await ShouldBeValidationError(response, "question", code);
    }

    [Fact]
    public async Task Post_Participant_Returns403()
    {
        using var response = await Participant.PostAsJsonAsync($"/api/polls/{PollId}/questions", new { question = "x" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.Forbidden, "poll.owner_required");
    }

    [Fact]
    public async Task Put_Question_UpdatesTextAndKeepsAnswers()
    {
        var questionId = await Owner.AddQuestionAsync(PollId, Ct, "old");
        await Participant.SaveAnswerAsync(PollId, questionId, "my answer", Ct);

        using var response = await Owner.PutAsJsonAsync($"/api/polls/{PollId}/questions/{questionId}", new { question = "new" }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var question = (await Participant.GetPollAsync(PollId, Ct)).Questions.Single();
        question.Question.Should().Be("new");
        question.MyAnswer.Should().Be("my answer");
    }

    [Fact]
    public async Task PutAndDelete_QuestionOfAnotherPoll_Return404QuestionNotFound()
    {
        var otherPoll = await Owner.CreatePollAsync(Ct);
        var foreignQuestion = await Owner.AddQuestionAsync(otherPoll, Ct);

        using var put = await Owner.PutAsJsonAsync($"/api/polls/{PollId}/questions/{foreignQuestion}", new { question = "x" }, Ct);
        using var delete = await Owner.DeleteAsync($"/api/polls/{PollId}/questions/{foreignQuestion}", Ct);

        await ShouldBeProblem(put, HttpStatusCode.NotFound, "question.not_found");
        await ShouldBeProblem(delete, HttpStatusCode.NotFound, "question.not_found");
    }

    [Fact]
    public async Task Delete_Question_HidesItAndNextQuestionGoesToTheEnd()
    {
        await Owner.AddQuestionAsync(PollId, Ct, "a");
        var b = await Owner.AddQuestionAsync(PollId, Ct, "b");

        using var response = await Owner.DeleteAsync($"/api/polls/{PollId}/questions/{b}", Ct);
        await Owner.AddQuestionAsync(PollId, Ct, "c");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Owner.GetPollAsync(PollId, Ct)).Questions.Select(q => (q.SortOrder, q.Question))
            .Should().Equal((1, "a"), (2, "c"));
    }

    [Fact]
    public async Task PutOrder_AllQuestions_RenumbersOneToN()
    {
        var a = await Owner.AddQuestionAsync(PollId, Ct, "a");
        var b = await Owner.AddQuestionAsync(PollId, Ct, "b");
        var c = await Owner.AddQuestionAsync(PollId, Ct, "c");

        using var response = await Owner.PutAsJsonAsync($"/api/polls/{PollId}/questions/order", new { questionIds = new[] { c, a, b } }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Owner.GetPollAsync(PollId, Ct)).Questions.Select(q => (q.SortOrder, q.Question))
            .Should().Equal((1, "c"), (2, "a"), (3, "b"));
    }

    [Fact]
    public async Task PutOrder_NotExactlyTheQuestions_Returns409OrderMismatch()
    {
        var a = await Owner.AddQuestionAsync(PollId, Ct);
        var b = await Owner.AddQuestionAsync(PollId, Ct);

        int[][] wrongOrders = [[a], [a, b, 999_999], [a, a]];
        foreach (var order in wrongOrders)
        {
            using var response = await Owner.PutAsJsonAsync($"/api/polls/{PollId}/questions/order", new { questionIds = order }, Ct);
            await ShouldBeProblem(response, HttpStatusCode.Conflict, "questions.order_mismatch");
        }
    }

    [Fact]
    public async Task PutOrder_MissingList_Returns400()
    {
        using var response = await Owner.PutAsJsonAsync($"/api/polls/{PollId}/questions/order", new { }, Ct);

        await ShouldBeValidationError(response, "questionIds", "questionIds.required");
    }
}
