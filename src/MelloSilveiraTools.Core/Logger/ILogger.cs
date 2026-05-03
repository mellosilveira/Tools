using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Logger;

/// <summary>
/// Abstraction for application logging with support for tags, exceptions and structured additional data.
/// Implementations decide the underlying sink (local file, remote service, etc.).
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Writes an error-level log entry. The caller file and member name are captured automatically to build tags.
    /// </summary>
    void Error(string message, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");

    /// <summary>
    /// Writes an error-level log entry with an associated exception.
    /// </summary>
    void Error(string message, Exception? ex, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");

    /// <summary>
    /// Writes an error-level log entry with an exception and structured <paramref name="additionalData"/>.
    /// </summary>
    void Error(string message, Exception? ex, IDictionary<string, object?> additionalData, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");

    /// <summary>
    /// Writes an error-level log entry using explicit <paramref name="tags"/> and <paramref name="additionalData"/>.
    /// </summary>
    void Error(string message, Exception? ex, IList<string> tags, IDictionary<string, object?> additionalData);

    /// <summary>
    /// Writes a warning-level log entry. The caller file and member name are captured automatically to build tags.
    /// </summary>
    void Warn(string message, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");

    /// <summary>
    /// Writes a warning-level log entry with an associated exception.
    /// </summary>
    void Warn(string message, Exception? ex, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");

    /// <summary>
    /// Writes a warning-level log entry with an exception and structured <paramref name="additionalData"/>.
    /// </summary>
    void Warn(string message, Exception? ex, IDictionary<string, object?> additionalData, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "");

    /// <summary>
    /// Writes a warning-level log entry using explicit <paramref name="tags"/> and <paramref name="additionalData"/>.
    /// </summary>
    void Warn(string message, Exception? ex, IDictionary<string, object?> additionalData, IList<string> tags);
}
