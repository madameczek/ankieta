namespace Polls.Api.Polls.Domain;

/// <summary>Lengths shared by the EF model and validators (D52).</summary>
internal static class PollLimits
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int QuestionMaxLength = 1000;
    public const int AnswerMaxLength = 4000;
}
