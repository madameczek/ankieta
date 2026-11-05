using System.Net;
using AwesomeAssertions;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Polls;

/// <summary>Three clients with their own sessions: the owner, a participant and an outsider, plus a poll owned by the owner.</summary>
public abstract class PollTestBase(SqlServerFixture database) : IAsyncLifetime
{
    protected PollsApiFactory Factory { get; } = new(database.ConnectionString);

    protected SqlServerFixture Database { get; } = database;

    protected HttpClient Owner { get; private set; } = null!;

    protected HttpClient Participant { get; private set; } = null!;

    protected HttpClient Outsider { get; private set; } = null!;

    protected UserDto OwnerUser { get; private set; } = null!;

    protected UserDto ParticipantUser { get; private set; } = null!;

    protected UserDto OutsiderUser { get; private set; } = null!;

    protected int PollId { get; private set; }

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        (Owner, OwnerUser) = await NewUserClientAsync();
        (Participant, ParticipantUser) = await NewUserClientAsync();
        (Outsider, OutsiderUser) = await NewUserClientAsync();

        PollId = await Owner.CreatePollAsync(Ct);
        await Owner.AddParticipantAsync(PollId, ParticipantUser.Id, Ct);
    }

    public async ValueTask DisposeAsync()
    {
        Owner.Dispose();
        Participant.Dispose();
        Outsider.Dispose();
        await Factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected async Task<(HttpClient Client, UserDto User)> NewUserClientAsync()
    {
        var client = Factory.CreateClient();
        return (client, await client.CreateUserAsync(Ct));
    }

    protected static async Task ShouldBeProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.Should().Be(status);
        (await response.ReadProblemCodeAsync(Ct)).Should().Be(code);
    }

    protected static async Task ShouldBeValidationError(HttpResponseMessage response, string field, string code)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (problemCode, errors) = await response.ReadValidationProblemAsync(Ct);
        problemCode.Should().Be("validation.failed");
        errors.Should().ContainKey(field).WhoseValue.Should().Equal(code);
    }
}
