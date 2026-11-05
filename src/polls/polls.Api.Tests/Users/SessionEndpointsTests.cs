using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Users;

public sealed class SessionEndpointsTests(SqlServerFixture database) : IAsyncDisposable
{
    private readonly PollsApiFactory _factory = new(database.ConnectionString);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_Visitor_ReturnsNullUser()
    {
        using var client = _factory.CreateClient();

        (await client.GetSessionUserAsync(Ct)).Should().BeNull();
    }

    [Fact]
    public async Task Post_ExistingUser_EntersUserContextWithPersistentCookie()
    {
        using var creator = _factory.CreateClient();
        var user = await creator.CreateUserAsync(Ct);
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync($"/api/session/{user.Id}", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<UserDto>(Ct)).Should().Be(user);
        (await client.GetSessionUserAsync(Ct)).Should().Be(user);

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("polls_session=", StringComparison.Ordinal)).ToLowerInvariant();
        cookie.Should().Contain("httponly").And.Contain("samesite=lax").And.Contain("expires=");
        var expires = DateTimeOffset.Parse(cookie.Split("expires=")[1].Split(';')[0], System.Globalization.CultureInfo.InvariantCulture);
        expires.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(365), TimeSpan.FromDays(1));
    }

    [Fact]
    public async Task Post_SwitchToAnotherUser_ReplacesSession()
    {
        using var client = _factory.CreateClient();
        await client.CreateUserAsync(Ct);
        using var other = _factory.CreateClient();
        var second = await other.CreateUserAsync(Ct);

        using var response = await client.PostAsync($"/api/session/{second.Id}", null, Ct);

        response.EnsureSuccessStatusCode();
        (await client.GetSessionUserAsync(Ct)).Should().Be(second);
    }

    [Fact]
    public async Task Post_UnknownUser_Returns404UserNotFound()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync($"/api/session/{int.MaxValue}", null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadProblemCodeAsync(Ct)).Should().Be("user.not_found");
    }

    [Fact]
    public async Task Delete_User_LeavesToVisitor()
    {
        using var client = _factory.CreateClient();
        await client.CreateUserAsync(Ct);

        using var response = await client.DeleteAsync("/api/session", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetSessionUserAsync(Ct)).Should().BeNull();
    }

    [Fact]
    public async Task Get_TamperedCookie_TreatedAsVisitor()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "polls_session=not-a-valid-ticket");

        (await client.GetSessionUserAsync(Ct)).Should().BeNull();
    }

    [Fact]
    public async Task Get_CookieOfMissingUser_TreatedAsVisitorAndCookieCleared()
    {
        using var client = _factory.CreateClient();
        var user = await client.CreateUserAsync(Ct);
        await using (var db = database.CreateContext())
        {
            // The app never deletes users (D6); this simulates a reset database.
            await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync(Ct);
        }

        using var response = await client.GetAsync("/api/session", Ct);

        (await client.GetSessionUserAsync(Ct)).Should().BeNull();
        response.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("polls_session=;", StringComparison.Ordinal));
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
