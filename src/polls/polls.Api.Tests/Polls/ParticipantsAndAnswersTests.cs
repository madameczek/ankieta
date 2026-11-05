using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Polls;

public sealed class ParticipantsAndAnswersTests(SqlServerFixture database) : PollTestBase(database)
{
    [Fact]
    public async Task GetParticipants_Owner_OwnerFirstWithAnswerCounts()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);
        await Participant.SaveAnswerAsync(PollId, question, "x", Ct);

        var participants = await Owner.GetParticipantsAsync(PollId, Ct);

        participants.Should().Equal(
            new ParticipantDto(OwnerUser.Id, OwnerUser.Name, true, 0),
            new ParticipantDto(ParticipantUser.Id, ParticipantUser.Name, false, 1));
    }

    [Fact]
    public async Task GetParticipants_Participant_Returns403()
    {
        using var response = await Participant.GetAsync($"/api/polls/{PollId}/participants", Ct);

        await ShouldBeProblem(response, HttpStatusCode.Forbidden, "poll.owner_required");
    }

    [Fact]
    public async Task PostParticipant_NewUser_GetsAccessAndIsIdempotent()
    {
        await Owner.AddParticipantAsync(PollId, OutsiderUser.Id, Ct);
        await Owner.AddParticipantAsync(PollId, OutsiderUser.Id, Ct);

        (await Outsider.GetPollAsync(PollId, Ct)).Role.Should().Be("participant");
        (await Owner.GetParticipantsAsync(PollId, Ct)).Should().HaveCount(3);
    }

    [Fact]
    public async Task PostParticipant_UnknownUser_Returns404UserNotFound()
    {
        using var response = await Owner.PostAsJsonAsync($"/api/polls/{PollId}/participants", new { userId = int.MaxValue }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "user.not_found");
    }

    [Fact]
    public async Task DeleteParticipant_Owner_Returns409IsOwner()
    {
        using var response = await Owner.DeleteAsync($"/api/polls/{PollId}/participants/{OwnerUser.Id}", Ct);

        await ShouldBeProblem(response, HttpStatusCode.Conflict, "participant.is_owner");
    }

    [Fact]
    public async Task DeleteParticipant_RevokesAccessHidesAnswersAndReAddingRestoresThem()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);
        await Participant.SaveAnswerAsync(PollId, question, "kept", Ct);

        using var response = await Owner.DeleteAsync($"/api/polls/{PollId}/participants/{ParticipantUser.Id}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var denied = await Participant.GetAsync($"/api/polls/{PollId}", Ct);
        await ShouldBeProblem(denied, HttpStatusCode.NotFound, "poll.not_found");
        (await Owner.GetResultsAsync(PollId, Ct)).Questions.Single().Answers.Should().BeEmpty();

        await Owner.AddParticipantAsync(PollId, ParticipantUser.Id, Ct);
        (await Owner.GetResultsAsync(PollId, Ct)).Questions.Single().Answers.Single().Answer.Should().Be("kept");
    }

    [Fact]
    public async Task PutAnswer_Participant_SavesAndUpdatesOwnAnswer()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);

        using var first = await Participant.PutAsJsonAsync($"/api/polls/{PollId}/questions/{question}/answer", new { answer = "first" }, Ct);
        await Participant.SaveAnswerAsync(PollId, question, "second", Ct);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = (await Participant.GetPollAsync(PollId, Ct)).Questions.Single();
        view.MyAnswer.Should().Be("second");
        view.MyAnswerModifiedAt.Should().NotBeNull();
        (await Owner.GetPollAsync(PollId, Ct)).Questions.Single().MyAnswer.Should().BeNull("each user sees only their own answer (D24)");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PutAnswer_Empty_DeletesAnswerAndReturns204(string empty)
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);
        await Participant.SaveAnswerAsync(PollId, question, "to be cleared", Ct);

        using var response = await Participant.PutAsJsonAsync($"/api/polls/{PollId}/questions/{question}/answer", new { answer = empty }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Participant.GetPollAsync(PollId, Ct)).Questions.Single().MyAnswer.Should().BeNull();
    }

    [Fact]
    public async Task PutAnswer_TooLong_Returns400()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);

        using var response = await Participant.PutAsJsonAsync($"/api/polls/{PollId}/questions/{question}/answer", new { answer = new string('x', 4001) }, Ct);

        await ShouldBeValidationError(response, "answer", "answer.too_long");
    }

    [Fact]
    public async Task PutAnswer_Outsider_Returns404PollNotFound()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);

        using var response = await Outsider.PutAsJsonAsync($"/api/polls/{PollId}/questions/{question}/answer", new { answer = "x" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "poll.not_found");
    }

    [Fact]
    public async Task PutAnswer_DeletedQuestion_Returns404QuestionNotFound()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);
        (await Owner.DeleteAsync($"/api/polls/{PollId}/questions/{question}", Ct)).Dispose();

        using var response = await Participant.PutAsJsonAsync($"/api/polls/{PollId}/questions/{question}/answer", new { answer = "x" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "question.not_found");
    }

    [Fact]
    public async Task GetResults_Owner_AnswersPerQuestionInOrder()
    {
        var q1 = await Owner.AddQuestionAsync(PollId, Ct, "q1");
        var q2 = await Owner.AddQuestionAsync(PollId, Ct, "q2");
        await Participant.SaveAnswerAsync(PollId, q1, "p-q1", Ct);
        await Owner.SaveAnswerAsync(PollId, q1, "o-q1", Ct);

        var results = await Owner.GetResultsAsync(PollId, Ct);

        results.ParticipantCount.Should().Be(2);
        results.Questions.Select(q => q.Id).Should().Equal(q1, q2);
        results.Questions[0].Answers.Select(a => (a.UserId, a.Answer)).Should().BeEquivalentTo(
            [(OwnerUser.Id, "o-q1"), (ParticipantUser.Id, "p-q1")]);
        results.Questions[1].Answers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResults_Participant_Returns403()
    {
        using var response = await Participant.GetAsync($"/api/polls/{PollId}/results", Ct);

        await ShouldBeProblem(response, HttpStatusCode.Forbidden, "poll.owner_required");
    }
}
