using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Infrastructure.Logger;

public class LocalFileLogger : LoggerBase
{
    protected override void WriteLog(string message, LogLevel logLevel, Exception? ex = null, IList<string>? tags = null, IDictionary<string, object>? additionalData = null)
    {
        throw new NotImplementedException();
    }
}
