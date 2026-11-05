namespace Polls.Api.Common.Paging;

internal sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

internal static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public const string PageInvalid = "page.invalid";
    public const string PageSizeInvalid = "pageSize.invalid";
}
