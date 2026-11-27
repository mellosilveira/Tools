namespace MelloSilveiraTools.WebApi.Authentication;

/// <summary>
/// Represents public identity and privilege metadata for the currently authenticated caller.
/// </summary>
/// <param name="UserId">Unique database identifier of the authenticated user.</param>
/// <param name="Username">Unique username of the authenticated user.</param>
/// <param name="IsSuperuser">Whether the authenticated user currently holds superuser privileges.</param>
public sealed record CurrentUserInfo(long UserId, string Username, bool IsSuperuser);
