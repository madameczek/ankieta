using AwesomeAssertions;
using Polls.Api.Common.Email;

namespace Polls.Api.Tests.Common.Email;

public sealed class EmailApiOptionsTests
{
    private const string Url = "http://mail.example.test/api/email/send";
    private const string Key = "secret-key";
    private const string From = "polls@example.test";
    private const string To = "admin@example.test";

    [Theory]
    [InlineData(Url, Key, From, To, true)]
    [InlineData("from-secrets", Key, From, To, false)]
    [InlineData("", Key, From, To, false)]
    [InlineData("ftp://mail.example.test/send", Key, From, To, false)]
    [InlineData(Url, "from-secrets", From, To, false)]
    [InlineData(Url, "", From, To, false)]
    [InlineData(Url, "  ", From, To, false)]
    [InlineData(Url, Key, "from-secrets", To, false)]
    [InlineData(Url, Key, "", To, false)]
    [InlineData(Url, Key, "Polls <polls@example.test>", To, false)]
    [InlineData(Url, Key, From, "from-secrets", false)]
    [InlineData(Url, Key, From, "", false)]
    [InlineData(Url, Key, From, "Admin <admin@example.test>", false)]
    public void IsConfigured_Settings_TrueOnlyWhenAllValid(string url, string apiKey, string from, string to, bool expected) =>
        new EmailApiOptions { Url = url, ApiKey = apiKey, From = from, To = to }.IsConfigured.Should().Be(expected);

    [Theory]
    [InlineData("polls", "Subject", "[polls] Subject")]
    [InlineData(null, "Subject", "Subject")]
    [InlineData("", "Subject", "Subject")]
    public void FormatSubject_Tag_PrefixesOnlyWhenSet(string? tag, string subject, string expected) =>
        new EmailApiOptions { SubjectTag = tag }.FormatSubject(subject).Should().Be(expected);
}
