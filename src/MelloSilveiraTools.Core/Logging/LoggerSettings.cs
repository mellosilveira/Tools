namespace MelloSilveiraTools.Core.Logging;

/// <summary>
/// Configuration for the Serilog-backed logging pipeline, including target directory, file naming, 
/// rotation policy, retention, and PostgreSQL batch-insert settings.
/// </summary>
/// <remarks>
/// Register a configured instance with <c>services.AddSingleton(new LoggerSettings { ... })</c> before calling
/// <c>AddCoreServices</c>; otherwise <c>AddCoreServices</c> registers an instance with default values.
/// </remarks>
public record LoggerSettings
{
    // --------------------------------------------------------
    // FILE SINK SETTINGS
    // --------------------------------------------------------

    /// <summary>
    /// Directory where log files are written. Created on logger startup if missing.
    /// Defaults to <c>{AppContext.BaseDirectory}/logs</c>.
    /// </summary>
    public string Directory { get; init; } = Path.Combine(AppContext.BaseDirectory, "logs");

    /// <summary>
    /// File name prefix (without extension). Defaults to <c>"log"</c>.
    /// </summary>
    public string FileNamePrefix { get; init; } = "log";

    /// <summary>
    /// When <c>true</c>, the active file rolls every UTC day and the date is embedded in the file name as
    /// <c>{FileNamePrefix}yyyyMMdd.txt</c>. Defaults to <c>true</c>.
    /// </summary>
    public bool RollDaily { get; init; } = true;

    /// <summary>
    /// Maximum size, in bytes, after which the active file is rolled to a new suffixed file. 
    /// Set to <c>null</c> to disable size-based rolling. Defaults to 10 MB.
    /// </summary>
    public long? MaxFileSizeBytes { get; init; } = 10L * 1024 * 1024;

    /// <summary>
    /// Maximum number of log files to retain in <see cref="Directory"/>. Older files are deleted on every roll.
    /// Set to <c>0</c> or negative to disable retention cleanup. Defaults to 30.
    /// </summary>
    public int MaxRetainedFiles { get; init; } = 30;

    // --------------------------------------------------------
    // POSTGRESQL SINK SETTINGS
    // --------------------------------------------------------

    /// <summary>
    /// Connection string for the PostgreSQL database. 
    /// If left null or empty, the application will only log to local files.
    /// </summary>
    public string? PostgreSqlConnectionString { get; init; }

    /// <summary>
    /// The name of the schema where table was created.
    /// </summary>
    public string? SchemaName { get; init; }

    /// <summary>
    /// The name of the table where logs will be stored.
    /// </summary>
    public string? TableName { get; init; }

    /// <summary>
    /// Limits how many logs are held in memory before a batch insert is triggered to PostgreSQL. Defaults to 50.
    /// </summary>
    public int BatchSizeLimit { get; init; } = 50;
}
