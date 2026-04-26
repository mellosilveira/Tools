using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Infrastructure.Logger;

/// <inheritdoc cref="ILogger"/>
public abstract class LoggerBase : ILogger
{
    /// <inheritdoc/>
    public void Error(string message, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        WriteLog(message, LogLevel.Error, tags: tags);
    }

    /// <inheritdoc/>
    public void Error(string message, Exception? ex, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        WriteLog(message, LogLevel.Error, ex, tags);
    }

    /// <inheritdoc/>
    public void Error(string message, Exception? ex, IDictionary<string, object?> additionalData, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        WriteLog(message, LogLevel.Error, ex, tags, additionalData);
    }

    /// <inheritdoc/>
    public void Error(string message, Exception? ex, IList<string> tags, IDictionary<string, object?> additionalData) => WriteLog(message, LogLevel.Error, ex, tags, additionalData);

    /// <inheritdoc/>
    public void Warn(string message, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        WriteLog(message, LogLevel.Warning, tags: tags);
    }

    /// <inheritdoc/>
    public void Warn(string message, Exception? ex, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        WriteLog(message, LogLevel.Warning, ex, tags);
    }

    /// <inheritdoc/>
    public void Warn(string message, Exception? ex, IDictionary<string, object?> additionalData, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        WriteLog(message, LogLevel.Warning, ex, tags, additionalData);
    }

    /// <inheritdoc/>
    public void Warn(string message, Exception? ex, IDictionary<string, object?> additionalData, IList<string> tags) => WriteLog(message, LogLevel.Warning, ex, tags, additionalData);

    /// <summary>
    /// Writes a single log entry to the underlying sink. Derived classes define how and where the entry is persisted.
    /// </summary>
    /// <param name="message">Human-readable log message.</param>
    /// <param name="logLevel">Severity of the entry.</param>
    /// <param name="ex">Optional exception associated with the entry.</param>
    /// <param name="tags">Optional tags used for filtering and indexing.</param>
    /// <param name="additionalData">Optional structured data to attach to the entry.</param>
    protected abstract void WriteLog(string message, LogLevel logLevel, Exception? ex = null, IList<string>? tags = null, IDictionary<string, object?>? additionalData = null);

    /// <summary>
    /// Builds the default tag set — the caller file name (without extension) and the caller member name.
    /// </summary>
    protected string[] BuildTags(string callerMemberName, string callerFilePath) => [Path.GetFileNameWithoutExtension(callerFilePath), callerMemberName];
}
