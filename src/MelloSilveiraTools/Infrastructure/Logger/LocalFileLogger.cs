using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Infrastructure.Logger;

/// <summary>
/// Default <see cref="ILogger"/> implementation intended to write log entries to a local file.
/// </summary>
/// <remarks>
/// <b>Placeholder no-op implementation.</b> All log writes are silently discarded. Provide your
/// own <see cref="ILogger"/> registration before relying on it in production.
/// </remarks>
[Obsolete("LocalFileLogger is a placeholder no-op implementation. Provide your own ILogger registration before relying on it in production.", false)]
public class LocalFileLogger : LoggerBase
{
    /// <inheritdoc/>
    protected override void WriteLog(string message, LogLevel logLevel, Exception? ex = null, IList<string>? tags = null, IDictionary<string, object>? additionalData = null)
    {
        return;
    }
}
