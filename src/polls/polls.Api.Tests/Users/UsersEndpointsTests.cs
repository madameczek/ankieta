using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Polls.Tests.Common;

namespace Polls.Api.Tests.Users;

public sealed class UsersEndpointsTests(SqlServerFixture database) : IAsyncDisposable
{
    private readonly PollsApiFactory _factory = new(database.ConnectionString);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Post_ValidName_Returns201AndEntersUserContext()
    {
        using var client = _factory.CreateClient();
        var name = TestData.Unique("  Ala") + "  ";

        using var response = await client.PostAsJsonAsync("/api/users", new { name }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(Ct);
        user!.Name.Should().Be(name.Trim());
        response.Headers.Location!.ToString().Should().Be($"/api/users/{user.Id}");
        (await client.GetSessionUserAsync(Ct)).Should().Be(user);
    }

    [Theory]
    [InlineData(null, "name.required")]
    [InlineData("", "name.required")]
    [InlineData("   ", "name.required")]
    public async Task Post_MissingName_Returns400NameRequired(string? name, string expectedCode)
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/users", new { name }, Ct);

        await ShouldBeValidationError(response, "name", expectedCode);
    }

    [Fact]
    public async Task Post_NameTooLong_Returns400NameTooLong()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/users", new { name = new string('x', 101) }, Ct);

        await ShouldBeValidationError(response, "name", "name.too_long");
    }

    [Fact]
    public async Task Post_NameTakenIgnoringCase_Returns409NameTaken()
    {
        using var client = _factory.CreateClient();
        var existing = await client.CreateUserAsync(Ct);

        using var response = await client.PostAsJsonAsync("/api/users", new { name = existing.Name.ToUpperInvariant() }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadProblemCodeAsync(Ct)).Should().Be("user.name_taken");
    }

    [Fact]
    public async Task Post_LockoutReached_Returns503UsersReached()
    {
        await using var locked = new PollsApiFactory(database.ConnectionString, new Dictionary<string, string?>
        {
            ["Thresholds:Users:Lockout"] = "0",
        });
        using var client = locked.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/users", new { name = TestData.Unique("user") }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await response.ReadProblemCodeAsync(Ct)).Should().Be("limit.users_reached");
    }

    [Fact]
    public async Task Post_CreateRateLimitExceeded_Returns429WithRetryAfter()
    {
        await using var limited = new PollsApiFactory(database.ConnectionString, new Dictionary<string, string?>
        {
            ["RateLimiting:Create:PermitLimit"] = "1",
        });
        using var client = limited.CreateClient();
        await client.CreateUserAsync(Ct);

        using var response = await client.PostAsJsonAsync("/api/users", new { name = TestData.Unique("user") }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
        (await response.ReadProblemCodeAsync(Ct)).Should().Be("rate_limited");
    }

    [Fact]
    public async Task Post_ClientIpHeaderConfigured_LimitsPerHeaderValue()
    {
        await using var limited = new PollsApiFactory(database.ConnectionString, new Dictionary<string, string?>
        {
            ["RateLimiting:Create:PermitLimit"] = "1",
            ["RateLimiting:ClientIpHeader"] = "X-Client-Ip",
        });
        using var client = limited.CreateClient();

        using var first = await PostUserFromAsync(client, "203.0.113.1");
        using var repeated = await PostUserFromAsync(client, "203.0.113.1");
        using var other = await PostUserFromAsync(client, "203.0.113.2");

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        repeated.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        other.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<HttpResponseMessage> PostUserFromAsync(HttpClient client, string clientIp)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        request.Content = JsonContent.Create(new { name = TestData.Unique("user") });
        request.Headers.Add("X-Client-Ip", clientIp);
        return await client.SendAsync(request, Ct);
    }

    [Fact]
    public async Task Get_Search_ReturnsMatchingUsersSortedAndPaged()
    {
        using var client = _factory.CreateClient();
        var tag = TestData.Unique("srch");
        foreach (var suffix in new[] { "c", "a", "b" })
        {
            await client.CreateUserAsync(Ct, $"{tag}-{suffix}");
        }

        var page = await client.GetFromJsonAsync<JsonElement>($"/api/users?search={tag}&page=2&pageSize=2", Ct);

        page.GetProperty("total").GetInt32().Should().Be(3);
        page.GetProperty("page").GetInt32().Should().Be(2);
        page.GetProperty("pageSize").GetInt32().Should().Be(2);
        page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString())
            .Should().Equal($"{tag}-c");
    }

    [Theory]
    [InlineData("page=0", "page", "page.invalid")]
    [InlineData("pageSize=0", "pageSize", "pageSize.invalid")]
    [InlineData("pageSize=101", "pageSize", "pageSize.invalid")]
    public async Task Get_InvalidPaging_Returns400(string query, string field, string expectedCode)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync($"/api/users?{query}", Ct);

        await ShouldBeValidationError(response, field, expectedCode);
    }

    [Fact]
    public async Task Get_SearchTooLong_Returns400()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync($"/api/users?search={new string('x', 101)}", Ct);

        await ShouldBeValidationError(response, "search", "search.too_long");
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task ShouldBeValidationError(HttpResponseMessage response, string field, string code)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var (problemCode, errors) = await response.ReadValidationProblemAsync(Ct);
        problemCode.Should().Be("validation.failed");
        errors.Should().ContainKey(field).WhoseValue.Should().Equal(code);
    }
}
