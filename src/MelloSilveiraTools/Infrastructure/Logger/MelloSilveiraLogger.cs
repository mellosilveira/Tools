using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Infrastructure.Logger;

/// <inheritdoc cref="ILogger"/>
public class LocalFileLogger : ILogger
{
    /// <inheritdoc/>
    public void Error(string message, Exception? ex = null, IDictionary<string, object>? additionalData = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        Error(message, ex, tags, additionalData);
    }

    /// <inheritdoc/>
    public void Warn(string message, Exception? ex = null, IDictionary<string, object>? additionalData = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        string[] tags = BuildTags(callerMemberName, callerFilePath);
        Warn(message, ex, tags, additionalData);
    }

    private void Error(string message, Exception? ex = null, IEnumerable<string>? tags = null, IDictionary<string, object>? additionalData = null)
    {
        throw new NotImplementedException();
    }

    private void Warn(string message, Exception? ex = null, IEnumerable<string>? tags = null, IDictionary<string, object>? additionalData = null)
    {
        throw new NotImplementedException();
    }

    private string[] BuildTags(string callerMemberName, string callerFilePath) => [Path.GetFileNameWithoutExtension(callerFilePath), callerMemberName];
}
