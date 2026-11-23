namespace MelloSilveiraTools.Database.Security;

/// <summary>
/// Manages and resolves the identifier of the currently authenticated user for PostgreSQL Row-Level Security (RLS)
/// session scoping and user-partitioned entity persistence.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// Returns the authenticated user's database identifier for the current asynchronous execution flow,
    /// or <see langword="null"/> when executing outside an authenticated user context
    /// (e.g., anonymous login/registration or system maintenance).
    /// </summary>
    /// <returns>The current user identifier, or <see langword="null"/> if unavailable.</returns>
    long? GetCurrentUserId();

    /// <summary>
    /// Establishes a user scope for the current asynchronous execution flow until the returned
    /// <see cref="IDisposable"/> is disposed, flowing automatically across <see cref="Task"/> continuations
    /// and background TPL Dataflow stages.
    /// </summary>
    /// <param name="userId">Authenticated user identifier, or <see langword="null"/> to clear the active user scope.</param>
    /// <returns>An <see cref="IDisposable"/> handle that restores the previous user scope upon disposal.</returns>
    IDisposable BeginScope(long? userId);
}
