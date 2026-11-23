namespace MelloSilveiraTools.Database.Security;

/// <summary>
/// Default <see cref="ICurrentUserProvider"/> backed by an instance-scoped <see cref="AsyncLocal{T}"/> slot so
/// Row-Level Security (RLS) user context flows automatically across asynchronous continuations,
/// <see cref="Task.Run(Action)"/>, and background TPL Dataflow stages without static global state.
/// Registered as Singleton in DI; safe for concurrent use across threads and requests.
/// </summary>
public sealed class CurrentUserProvider : ICurrentUserProvider
{
    private readonly AsyncLocal<long?> _current = new();

    /// <inheritdoc/>
    public long? GetCurrentUserId() => _current.Value;

    /// <inheritdoc/>
    public IDisposable BeginScope(long? userId)
    {
        long? previous = _current.Value;
        _current.Value = userId;
        return new ScopeHandle(_current, previous);
    }

    private sealed class ScopeHandle(AsyncLocal<long?> slot, long? previous) : IDisposable
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
