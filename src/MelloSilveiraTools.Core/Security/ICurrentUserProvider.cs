namespace MelloSilveiraTools.Core.Security;

/// <summary>
/// Manages and resolves the authenticated user identifier and superuser privilege for the current
/// asynchronous execution flow across cross-cutting isolation boundaries (such as PostgreSQL Row-Level Security,
/// pipeline progress channels, and administrative commands).
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// Returns the authenticated user's database identifier for the current asynchronous execution flow,
    /// or <see langword="null"/> when executing outside an authenticated user context.
    /// </summary>
    /// <returns>The current user identifier, or <see langword="null"/> if unavailable.</returns>
    long? GetCurrentUserId();

    /// <summary>
    /// Returns whether the current asynchronous execution flow is operating under superuser privileges.
    /// </summary>
    /// <returns><see langword="true"/> when executing as a superuser; otherwise <see langword="false"/>.</returns>
    bool IsSuperuser();

    /// <summary>
    /// Establishes a user and superuser privilege scope for the current asynchronous execution flow
    /// until the returned <see cref="IDisposable"/> is disposed.
    /// </summary>
    /// <param name="userId">Authenticated user identifier, or <see langword="null"/> to clear the active user scope.</param>
    /// <param name="isSuperuser">Whether the active scope carries superuser privileges.</param>
    /// <returns>An <see cref="IDisposable"/> handle that restores the previous scope upon disposal.</returns>
    IDisposable BeginScope(long? userId, bool isSuperuser = false);
}
