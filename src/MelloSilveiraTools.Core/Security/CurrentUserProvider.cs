namespace MelloSilveiraTools.Core.Security;

/// <summary>
/// Default <see cref="ICurrentUserProvider"/> backed by an instance-scoped <see cref="AsyncLocal{T}"/> slot so
/// user and superuser context flows automatically across asynchronous continuations,
/// <see cref="Task.Run(Action)"/>, and background TPL Dataflow stages without static global state.
/// Registered as Singleton in DI; safe for concurrent use across threads and requests.
/// </summary>
public sealed class CurrentUserProvider : ICurrentUserProvider
{
    private readonly AsyncLocal<UserScope> _current = new();

    /// <inheritdoc/>
    public long? GetCurrentUserId() => _current.Value.UserId;

    /// <inheritdoc/>
    public bool IsSuperuser() => _current.Value.IsSuperuser;

    /// <inheritdoc/>
    public IDisposable BeginScope(long? userId, bool isSuperuser = false)
    {
        UserScope previous = _current.Value;
        _current.Value = new UserScope(userId, isSuperuser);
        return new ScopeHandle(_current, previous);
    }

    private readonly record struct UserScope(long? UserId, bool IsSuperuser);

    private sealed class ScopeHandle(AsyncLocal<UserScope> slot, UserScope previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            slot.Value = previous;
        }
    }
}
