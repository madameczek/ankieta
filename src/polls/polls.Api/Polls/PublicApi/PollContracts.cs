namespace Polls.Api.Polls.PublicApi;

internal static class PollRoles
{
    public const string Owner = "owner";
    public const string Participant = "participant";

    public static string Of(bool isOwner) => isOwner ? Owner : Participant;
}

// Requests
internal sealed record PollRequest(string? Title, string? Description);

internal sealed record QuestionRequest(string? Question);

internal sealed record QuestionOrderRequest(IReadOnlyList<int>? QuestionIds);

internal sealed record ParticipantRequest(int? UserId);

internal sealed record AnswerRequest(string? Answer);

// Responses
internal sealed record CreatedPollResponse(int Id);

internal sealed record PollListItem(int Id, string Title, string Role, int Answered, int QuestionCount, DateTime ModifiedAt);

internal sealed record PollQuestionView(int Id, int SortOrder, string Question, string? MyAnswer, DateTime? MyAnswerModifiedAt);

internal sealed record PollDetailResponse(int Id, string Title, string? Description, string Role, IReadOnlyList<PollQuestionView> Questions);

internal sealed record CreatedQuestionResponse(int Id, int SortOrder);

internal sealed record ParticipantView(int UserId, string Name, bool IsOwner, int Answered);

internal sealed record SavedAnswerResponse(DateTime ModifiedAt);

internal sealed record ResultAnswer(int UserId, string Name, string Answer, DateTime ModifiedAt);

internal sealed record ResultQuestion(int Id, int SortOrder, string Question, IReadOnlyList<ResultAnswer> Answers);

internal sealed record PollResultsResponse(int ParticipantCount, IReadOnlyList<ResultQuestion> Questions);
