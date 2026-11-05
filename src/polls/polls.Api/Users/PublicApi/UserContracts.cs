namespace Polls.Api.Users.PublicApi;

internal sealed record CreateUserRequest(string? Name);

internal sealed record UserResponse(int Id, string Name);

internal sealed record UserListItem(int Id, string Name, DateTime CreatedAt);

/// <summary><c>User</c> is null for a visitor (D11).</summary>
internal sealed record SessionResponse(UserResponse? User);
