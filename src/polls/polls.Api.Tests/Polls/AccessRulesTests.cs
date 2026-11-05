using System.Net;
using System.Net.Http.Json;
using System.Text;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Polls;

/// <summary>D41 for every owner route, deleted polls and removed participants; access is checked before the body (404/403 before 400).</summary>
public sealed class AccessRulesTests(SqlServerFixture database) : PollTestBase(database)
{
    public static TheoryData<string, string> OwnerRoutes => new()
    {
        { "PUT", "/api/polls/{0}" },
        { "DELETE", "/api/polls/{0}" },
        { "POST", "/api/polls/{0}/questions" },
        { "PUT", "/api/polls/{0}/questions/order" },
        { "GET", "/api/polls/{0}/participants" },
        { "POST", "/api/polls/{0}/participants" },
        { "DELETE", "/api/polls/{0}/participants/1" },
        { "GET", "/api/polls/{0}/results" },
    };

    [Theory]
    [MemberData(nameof(OwnerRoutes))]
    public async Task OwnerRoute_Outsider_Returns404EvenWithInvalidBody(string method, string route)
    {
        using var response = await SendInvalidBodyAsync(Outsider, method, string.Format(route, PollId));

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "poll.not_found");
    }

    [Theory]
    [MemberData(nameof(OwnerRoutes))]
    public async Task OwnerRoute_Participant_Returns403EvenWithInvalidBody(string method, string route)
    {
        using var response = await SendInvalidBodyAsync(Participant, method, string.Format(route, PollId));

        await ShouldBeProblem(response, HttpStatusCode.Forbidden, "poll.owner_required");
    }

    [Theory]
    [MemberData(nameof(OwnerRoutes))]
    public async Task OwnerRoute_DeletedPoll_Returns404ForOwner(string method, string route)
    {
        (await Owner.DeleteAsync($"/api/polls/{PollId}", Ct)).Dispose();

        using var response = await SendInvalidBodyAsync(Owner, method, string.Format(route, PollId));

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "poll.not_found");
    }

    [Fact]
    public async Task OwnerRoute_DeleteQuestionAsParticipant_Returns403()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);

        using var response = await Participant.DeleteAsync($"/api/polls/{PollId}/questions/{question}", Ct);

        await ShouldBeProblem(response, HttpStatusCode.Forbidden, "poll.owner_required");
    }

    [Fact]
    public async Task PutAnswer_RemovedParticipant_Returns404()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);
        (await Owner.DeleteAsync($"/api/polls/{PollId}/participants/{ParticipantUser.Id}", Ct)).Dispose();

        using var response = await Participant.PutAsJsonAsync($"/api/polls/{PollId}/questions/{question}/answer", new { answer = "x" }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "poll.not_found");
    }

    [Fact]
    public async Task PutAnswer_OutsiderWithTooLongAnswer_Returns404NotValidation()
    {
        var question = await Owner.AddQuestionAsync(PollId, Ct);

        using var response = await Outsider.PutAsJsonAsync($"/api/polls/{PollId}/questions/{question}/answer", new { answer = new string('x', 4001) }, Ct);

        await ShouldBeProblem(response, HttpStatusCode.NotFound, "poll.not_found");
    }

    /// <summary>A body that fails validation for every route that has one (empty title/question/order/userId).</summary>
    private static async Task<HttpResponseMessage> SendInvalidBodyAsync(HttpClient client, string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "PUT" or "POST")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request, Ct);
    }
}
