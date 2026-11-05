namespace Polls.Api.Common.Email;

/// <summary><paramref name="Body"/> is plain text; the sender encodes it.</summary>
internal sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>Sends email (D17): <see cref="HttpEmailSender"/> when the mail API is configured, else <see cref="LoggingEmailSender"/>.</summary>
internal interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
