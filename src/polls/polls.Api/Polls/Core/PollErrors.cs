namespace Polls.Api.Polls.Core;

internal static class PollErrors
{
    public const string NotFound = "poll.not_found";
    public const string OwnerRequired = "poll.owner_required";
    public const string QuestionNotFound = "question.not_found";
    public const string OrderMismatch = "questions.order_mismatch";
    public const string ParticipantIsOwner = "participant.is_owner";

    public const string TitleRequired = "title.required";
    public const string TitleTooLong = "title.too_long";
    public const string DescriptionTooLong = "description.too_long";
    public const string QuestionRequired = "question.required";
    public const string QuestionTooLong = "question.too_long";
    public const string AnswerTooLong = "answer.too_long";
    public const string QuestionIdsRequired = "questionIds.required";
}
