using System.Net.Mail;

namespace Polls.Api.Common.Email;

/// <summary>Section <c>EmailApi</c> (D17): the LAN mail HTTP API. Values come from user secrets / the k8s secret.</summary>
internal sealed class EmailApiOptions
{
    public const string Section = "EmailApi";

    /// <summary>Value in <c>appsettings.json</c> for settings that must come from secrets.</summary>
    public const string SecretPlaceholder = "from-secrets";

    /// <summary>Full send endpoint; must be an absolute http(s) URL.</summary>
    public string Url { get; init; } = "";

    /// <summary>Sent in the <c>x-api-key</c> header; trimmed on load (secret files often end with a newline).</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Sender address; must be a valid address, the mail API rejects others with 400.</summary>
    public string From { get; init; } = "";

    /// <summary>Recipient of the threshold warning (D14); must be a valid address.</summary>
    public string To { get; init; } = "";

    /// <summary>Subject prefix <c>[tag] </c>. Empty: <c>Graylog:AppName</c>; both empty: no prefix.</summary>
    public string? SubjectTag { get; set; }

    public string Signature { get; init; } = "";

    /// <summary>HTTP timeout; must be positive (validated at startup).</summary>
    public int TimeoutSeconds { get; init; } = 10;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey) && ApiKey != SecretPlaceholder;

    /// <summary>Mail API usable: http(s) <see cref="Url"/>, an API key, valid <see cref="From"/> and <see cref="To"/>.</summary>
    public bool IsConfigured => IsHttpUrl(Url) && HasApiKey && IsValidEmail(From) && IsValidEmail(To);

    public string FormatSubject(string subject) =>
        string.IsNullOrWhiteSpace(SubjectTag) ? subject : $"[{SubjectTag}] {subject}";

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>A bare address (no display name), e.g. <c>name@example.com</c>.</summary>
    private static bool IsValidEmail(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && MailAddress.TryCreate(value, out var address)
        && address.Address == value;
}
