namespace MelloSilveiraTools.Core.Logging;

/// <summary>
/// Aggregated configuration settings providing a unified binding point for application logging sinks.
/// Enables simultaneous configuration of local file and database logging sinks.
/// </summary>
public sealed record LoggerSettings
{
    /// <summary>
    /// Gets the settings governing local file logging.
    /// </summary>
    public FileLoggerSettings? File { get; init; }

    /// <summary>
    /// Gets the settings governing database logging.
    /// </summary>
    public DatabaseLoggerSettings? Database { get; init; }
}
