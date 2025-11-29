using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Infrastructure.Logger;

public interface ILogger
{
    void Error(string message, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");
    void Error(string message, Exception? ex, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");
    void Error(string message, Exception? ex, IDictionary<string, object?> additionalData, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");
    void Error(string message, Exception? ex, IList<string> tags, IDictionary<string, object?> additionalData);

    void Warn(string message, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");
    void Warn(string message, Exception? ex, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");
    void Warn(string message, Exception? ex, IDictionary<string, object?> additionalData, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");
    void Warn(string message, Exception? ex, IDictionary<string, object?> additionalData, IList<string> tags);
}
