using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Polls;

public sealed class PollsEndpointsTests(SqlServerFixture database) : PollTestBase(database)
{
    [Fact]
    public async Task Post_Valid_Returns201AndCreatorIsOwner()
    {
        using var response = await Owner.PostAsJsonAsync("/api/polls", new { title = "  Lunch  ", description = "  where?  " }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<CreatedDto>(Ct))!.Id;
        response.Headers.Location!.ToString().Should().Be($"/api/polls/{id}");
        var poll = await Owner.GetPollAsync(id, Ct);
        poll.Should().BeEquivalentTo(new { Id = id, Title = "Lunch", Description = "where?", Role = "owner" });
        poll.Questions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, null, "title", "title.required")]
    [InlineData("  ", null, "title", "title.required")]
    [InlineData(201, null, "title", "title.too_long")]
    [InlineData("ok", 2001, "description", "description.too_long")]
    public async Task Post_Invalid_Returns400(object? title, object? description, string field, string code)
    {
        using var response = await Owner.PostAsJsonAsync("/api/polls", new { title = Text(title), description = Text(description) }, Ct);

        await ShouldBeValidationError(response, field, code);
    }

    [Fact]
    public async Task Post_Visitor_Returns401SessionRequired()
    {
        using var visitor = Factory.CreateClient();

        using var response = await visitor.PostAsJsonAsync("/api/polls", new { title = "x" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.Unauthorized, "session.required");
    }

    [Fact]
    public async Task Post_LockoutReached_Returns503PollsReached()
    {
        await using var locked = new PollsApiFactory(Database.ConnectionString, new Dictionary<string, string?> { ["Thresholds:Polls:Lockout"] = "0" });
        using var client = locked.CreateClient();
        await client.CreateUserAsync(Ct);

        using var response = await client.PostAsJsonAsync("/api/polls", new { title = "x" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.ServiceUnavailable, "limit.polls_reached");
    }

    [Fact]
    public async Task GetList_OwnedAndParticipating_WithRoleAndProgressNewestFirst()
    {
        var question1 = await Owner.AddQuestionAsync(PollId, Ct);
        await Owner.AddQuestionAsync(PollId, Ct);
        await Participant.SaveAnswerAsync(PollId, question1, "yes", Ct);
        var participantsOwnPoll = await Participant.CreatePollAsync(Ct);

        var list = await Participant.GetMyPollsAsync(Ct);

        list.Select(p => (p.Id, p.Role, p.Answered, p.QuestionCount)).Should().Equal(
            (participantsOwnPoll, "owner", 0, 0),
            (PollId, "participant", 1, 2));
    }

    [Fact]
    public async Task Get_Outsider_Returns404()
    {
        using var response = await Outsider.GetAsync($"/api/polls/{PollId}", Ct);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "poll.not_found");
    }

    [Fact]
    public async Task Get_Participant_ReturnsParticipantRole()
    {
        (await Participant.GetPollAsync(PollId, Ct)).Role.Should().Be("participant");
    }

    [Fact]
    public async Task Put_Owner_UpdatesTitleAndDescription()
    {
        using var response = await Owner.PutAsJsonAsync($"/api/polls/{PollId}", new { title = "New title", description = (string?)null }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Owner.GetPollAsync(PollId, Ct)).Should().BeEquivalentTo(new { Title = "New title", Description = (string?)null });
    }

    [Fact]
    public async Task Put_Participant_Returns403OwnerRequired()
    {
        using var response = await Participant.PutAsJsonAsync($"/api/polls/{PollId}", new { title = "hijack" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.Forbidden, "poll.owner_required");
    }

    [Fact]
    public async Task Put_Outsider_Returns404()
    {
        using var response = await Outsider.PutAsJsonAsync($"/api/polls/{PollId}", new { title = "x" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "poll.not_found");
    }

    [Fact]
    public async Task Delete_Owner_SoftDeletesAndHidesFromEveryone()
    {
        using var response = await Owner.DeleteAsync($"/api/polls/{PollId}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var afterDelete = await Participant.GetAsync($"/api/polls/{PollId}", Ct);
        await ShouldBeProblem(afterDelete, HttpStatusCode.NotFound, "poll.not_found");
        (await Owner.GetMyPollsAsync(Ct)).Should().NotContain(p => p.Id == PollId);
    }

    [Fact]
    public async Task Delete_Participant_Returns403()
    {
        using var response = await Participant.DeleteAsync($"/api/polls/{PollId}", Ct);

        await ShouldBeProblem(response, HttpStatusCode.Forbidden, "poll.owner_required");
    }

    /// <summary>Theory data: an int means "a string of that length".</summary>
    private static string? Text(object? value) => value is int length ? new string('x', length) : (string?)value;
}
