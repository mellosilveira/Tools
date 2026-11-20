namespace MelloSilveiraTools.Core.Logging;

/// <summary>
/// Configuration settings governing database logging sink, including connection string,
/// destination schema and table names, batch size limits, and flush periods.
/// </summary>
public sealed record DatabaseLoggerSettings
{
    /// <summary>
    /// Gets the connection string used to connect to the logging database.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Gets the name of the database table where log entries are stored.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets the optional schema name where the log table resides.
    /// </summary>
    public string? SchemaName { get; init; }

    /// <summary>
    /// Gets the optional limit on how many log events are held in memory before triggering a batch insert.
    /// </summary>
    public int? BatchSizeLimit { get; init; }

    /// <summary>
    /// Gets the optional maximum time to wait before flushing queued log events to the database.
    /// </summary>
    public TimeSpan? Period { get; init; }

    /// <summary>
    /// Gets the optional value indicating whether the logging sink should automatically create the log table if missing.
    /// </summary>
    public bool? NeedAutoCreateTable { get; init; }
}
