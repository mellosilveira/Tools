namespace MelloSilveiraTools.Core.Logging;

/// <summary>
/// Configuration settings governing local file logging sink, including target directory,
/// file naming, rotation policy, and retention limits.
/// </summary>
public sealed record FileLoggerSettings
{
    /// <summary>
    /// Gets the directory where log files are written. Created on logger startup if missing.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// Gets the file name prefix (without extension).
    /// </summary>
    public required string FileNamePrefix { get; init; }

    /// <summary>
    /// Gets a value indicating whether the active file rolls every UTC day with the date embedded in the file name as
    /// <c>{FileNamePrefix}yyyyMMdd.txt</c>.
    /// </summary>
    public required bool RollDaily { get; init; }

    /// <summary>
    /// Gets the maximum size, in bytes, after which the active file is rolled to a new suffixed file.
    /// When <c>null</c>, size-based rolling is disabled.
    /// </summary>
    public long? MaxFileSizeBytes { get; init; }

    /// <summary>
    /// Gets the maximum number of log files to retain in <see cref="Directory"/>. Older files are deleted on every roll.
    /// When <c>0</c> or negative, retention cleanup is disabled.
    /// </summary>
    public required int MaxRetainedFiles { get; init; }
}
