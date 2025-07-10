using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Infrastructure.Logger;

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

    protected abstract void WriteLog(string message, LogLevel logLevel, Exception? ex = null, IList<string>? tags = null, IDictionary<string, object?>? additionalData = null);

    protected string[] BuildTags(string callerMemberName, string callerFilePath) => [Path.GetFileNameWithoutExtension(callerFilePath), callerMemberName];
}
