using System.Security.Claims;
using Polls.Api.Common;
using Polls.Api.Common.Endpoints;
using Polls.Api.Common.Http;
using Polls.Api.Common.RateLimiting;
using Polls.Api.Polls.Core;
using Polls.Api.Polls.PublicApi;
using Polls.Api.Users.Infrastructure;

namespace Polls.Api.Polls.Endpoints;

/// <summary>All poll routes require a current user (D11); access per poll is checked by the handlers (D41).</summary>
internal sealed class PollsEndpoints : IApiEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var polls = app.MapGroup(RouteConsts.Polls).RequireAuthorization().WithTags("Polls");

        polls.MapGet("", ListAsync);
        polls.MapPost("", CreateAsync).RequireRateLimiting(RateLimitPolicies.Create);
        polls.MapGet("{pollId:int}", GetAsync);
        polls.MapPut("{pollId:int}", UpdateAsync);
        polls.MapDelete("{pollId:int}", DeleteAsync);

        var questions = polls.MapGroup("{pollId:int}/questions").WithTags("Questions");
        // D15: every create endpoint has the stricter policy
        questions.MapPost("", AddQuestionAsync).RequireRateLimiting(RateLimitPolicies.Create);
        questions.MapPut("order", ReorderAsync);
        questions.MapPut("{questionId:int}", UpdateQuestionAsync);
        questions.MapDelete("{questionId:int}", DeleteQuestionAsync);
        questions.MapPut("{questionId:int}/answer", SaveAnswerAsync).WithTags("Answers");

        var participants = polls.MapGroup("{pollId:int}/participants").WithTags("Participants");
        participants.MapGet("", ListParticipantsAsync);
        participants.MapPost("", AddParticipantAsync).RequireRateLimiting(RateLimitPolicies.Create);
        participants.MapDelete("{userId:int}", RemoveParticipantAsync);

        polls.MapGet("{pollId:int}/results", GetResultsAsync).WithTags("Results");
    }

    private static async Task<IResult> ListAsync(ClaimsPrincipal user, ListMyPollsHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateAsync(PollRequest request, ClaimsPrincipal user, CreatePollHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), new PollCommand(request.Title, request.Description), ct))
            .ToHttpResult(created => TypedResults.Created($"{RouteConsts.Polls}/{created.Id}", created));

    private static async Task<IResult> GetAsync(int pollId, ClaimsPrincipal user, GetPollHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> UpdateAsync(int pollId, PollRequest request, ClaimsPrincipal user, UpdatePollHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, new PollCommand(request.Title, request.Description), ct))
            .ToHttpResult(TypedResults.NoContent);

    private static async Task<IResult> DeleteAsync(int pollId, ClaimsPrincipal user, DeletePollHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, ct)).ToHttpResult(TypedResults.NoContent);

    private static async Task<IResult> AddQuestionAsync(int pollId, QuestionRequest request, ClaimsPrincipal user, AddQuestionHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, new QuestionCommand(request.Question), ct))
            .ToHttpResult(created => TypedResults.Created($"{RouteConsts.Polls}/{pollId}/questions/{created.Id}", created));

    private static async Task<IResult> UpdateQuestionAsync(
        int pollId, int questionId, QuestionRequest request, ClaimsPrincipal user, UpdateQuestionHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, questionId, new QuestionCommand(request.Question), ct))
            .ToHttpResult(TypedResults.NoContent);

    private static async Task<IResult> DeleteQuestionAsync(int pollId, int questionId, ClaimsPrincipal user, DeleteQuestionHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, questionId, ct)).ToHttpResult(TypedResults.NoContent);

    private static async Task<IResult> ReorderAsync(int pollId, QuestionOrderRequest request, ClaimsPrincipal user, ReorderQuestionsHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, new QuestionOrderCommand(request.QuestionIds), ct))
            .ToHttpResult(TypedResults.NoContent);

    private static async Task<IResult> SaveAnswerAsync(
        int pollId, int questionId, AnswerRequest request, ClaimsPrincipal user, SaveAnswerHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, questionId, new AnswerCommand(request.Answer), ct))
            .ToHttpResult(saved => saved is null ? TypedResults.NoContent() : TypedResults.Ok(saved));

    private static async Task<IResult> ListParticipantsAsync(int pollId, ClaimsPrincipal user, ListParticipantsHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> AddParticipantAsync(
        int pollId, ParticipantRequest request, ClaimsPrincipal user, AddParticipantHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, new AddParticipantCommand(request.UserId), ct)).ToHttpResult(TypedResults.NoContent);

    private static async Task<IResult> RemoveParticipantAsync(int pollId, int userId, ClaimsPrincipal user, RemoveParticipantHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, userId, ct)).ToHttpResult(TypedResults.NoContent);

    private static async Task<IResult> GetResultsAsync(int pollId, ClaimsPrincipal user, GetResultsHandler handler, CancellationToken ct) =>
        (await handler.HandleAsync(user.RequiredUserId(), pollId, ct)).ToHttpResult(TypedResults.Ok);
}
