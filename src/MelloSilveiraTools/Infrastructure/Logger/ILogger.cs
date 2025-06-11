namespace MelloSilveiraTools.Infrastructure.Logger;

public interface ILogger
{
    void Error(string message, Exception? ex = null, IEnumerable<string>? tags = null, IDictionary<string, object?>? additionalData = null);

    void Warn(string message, Exception? ex = null, IEnumerable<string>? tags = null, IDictionary<string, object?>? additionalData = null);
}
