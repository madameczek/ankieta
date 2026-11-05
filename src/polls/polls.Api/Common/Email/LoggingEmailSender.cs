namespace Polls.Api.Common.Email;

/// <summary>Stub (D17): logs the message instead of sending it while the <c>EmailApi</c> settings are invalid (local runs, tests).</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Email not sent (stub). To {EmailTo}, subject {EmailSubject}, body {EmailBody}",
            message.To,
            message.Subject,
            message.Body);

        return Task.CompletedTask;
    }
}
