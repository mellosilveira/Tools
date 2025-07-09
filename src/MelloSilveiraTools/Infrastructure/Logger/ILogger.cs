using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Infrastructure.Logger;

public interface ILogger
{
    void Error(string message, Exception? ex = null, IDictionary<string, object>? additionalData = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");

    void Warn(string message, Exception? ex = null, IDictionary<string, object>? additionalData = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");
}
