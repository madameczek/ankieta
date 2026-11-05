using System.Net.Http.Json;
using System.Text.Json;

namespace Polls.Tests.Common;

public sealed record CreatedDto(int Id);

public sealed record QuestionDto(int Id, int SortOrder, string Question, string? MyAnswer, DateTime? MyAnswerModifiedAt);

public sealed record PollDto(int Id, string Title, string? Description, string Role, IReadOnlyList<QuestionDto> Questions);

public sealed record PollListItemDto(int Id, string Title, string Role, int Answered, int QuestionCount, DateTime ModifiedAt);

public sealed record ParticipantDto(int UserId, string Name, bool IsOwner, int Answered);

public sealed record ResultAnswerDto(int UserId, string Name, string Answer, DateTime ModifiedAt);

public sealed record ResultQuestionDto(int Id, int SortOrder, string Question, IReadOnlyList<ResultAnswerDto> Answers);

public sealed record ResultsDto(int ParticipantCount, IReadOnlyList<ResultQuestionDto> Questions);

/// <summary>Happy-path calls for poll tests; each throws on a non-success status.</summary>
public static class PollApiExtensions
{
    public static async Task<int> CreatePollAsync(this HttpClient client, CancellationToken ct, string? title = null, string? description = null)
    {
        using var response = await client.PostAsJsonAsync("/api/polls", new { title = title ?? TestData.Unique("poll"), description }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedDto>(ct))!.Id;
    }

    public static async Task<int> AddQuestionAsync(this HttpClient client, int pollId, CancellationToken ct, string? question = null)
    {
        using var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/questions", new { question = question ?? TestData.Unique("q") }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedDto>(ct))!.Id;
    }

    public static async Task AddParticipantAsync(this HttpClient client, int pollId, int userId, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/participants", new { userId }, ct);
        response.EnsureSuccessStatusCode();
    }

    public static async Task SaveAnswerAsync(this HttpClient client, int pollId, int questionId, string answer, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync($"/api/polls/{pollId}/questions/{questionId}/answer", new { answer }, ct);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<PollDto> GetPollAsync(this HttpClient client, int pollId, CancellationToken ct) =>
        (await client.GetFromJsonAsync<PollDto>($"/api/polls/{pollId}", JsonSerializerOptions.Web, ct))!;

    public static async Task<IReadOnlyList<PollListItemDto>> GetMyPollsAsync(this HttpClient client, CancellationToken ct) =>
        (await client.GetFromJsonAsync<List<PollListItemDto>>("/api/polls", JsonSerializerOptions.Web, ct))!;

    public static async Task<ResultsDto> GetResultsAsync(this HttpClient client, int pollId, CancellationToken ct) =>
        (await client.GetFromJsonAsync<ResultsDto>($"/api/polls/{pollId}/results", JsonSerializerOptions.Web, ct))!;

    public static async Task<IReadOnlyList<ParticipantDto>> GetParticipantsAsync(this HttpClient client, int pollId, CancellationToken ct) =>
        (await client.GetFromJsonAsync<List<ParticipantDto>>($"/api/polls/{pollId}/participants", JsonSerializerOptions.Web, ct))!;
}
