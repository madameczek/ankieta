namespace Polls.Api.Users.Core;

internal static class UserErrors
{
    public const string NameRequired = "name.required";
    public const string NameTooLong = "name.too_long";
    public const string SearchTooLong = "search.too_long";
    public const string NameTaken = "user.name_taken";
    public const string NotFound = "user.not_found";
    public const string SessionRequired = "session.required";
}
