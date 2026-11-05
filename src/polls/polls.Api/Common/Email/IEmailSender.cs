namespace Polls.Api.Common.Email;

internal sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>Sends email through the mail HTTP API (D17). Stubbed until the API contract is known.</summary>
internal interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
