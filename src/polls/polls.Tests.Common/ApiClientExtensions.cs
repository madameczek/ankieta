using System.Net.Http.Json;
using System.Text.Json;

namespace Polls.Tests.Common;

public sealed record UserDto(int Id, string Name);

public static class ApiClientExtensions
{
    /// <summary>The <c>code</c> extension of a ProblemDetails response.</summary>
    public static async Task<string?> ReadProblemCodeAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return problem.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary><c>code</c> and field → error codes of a validation ProblemDetails response (the body is read once).</summary>
    public static async Task<(string? Code, Dictionary<string, string[]> Errors)> ReadValidationProblemAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var code = problem.TryGetProperty("code", out var c) ? c.GetString() : null;
        var errors = problem.TryGetProperty("errors", out var e) ? e.Deserialize<Dictionary<string, string[]>>() ?? [] : [];
        return (code, errors);
    }

    /// <summary>Creates a user with a unique name; the client then carries that user's session cookie.</summary>
    public static async Task<UserDto> CreateUserAsync(this HttpClient client, CancellationToken cancellationToken, string? name = null)
    {
        using var response = await client.PostAsJsonAsync("/api/users", new { name = name ?? TestData.Unique("user") }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken))!;
    }

    public static async Task<UserDto?> GetSessionUserAsync(this HttpClient client, CancellationToken cancellationToken)
    {
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session", cancellationToken);
        var user = session.GetProperty("user");
        return user.ValueKind == JsonValueKind.Null ? null : user.Deserialize<UserDto>(JsonSerializerOptions.Web);
    }
}
