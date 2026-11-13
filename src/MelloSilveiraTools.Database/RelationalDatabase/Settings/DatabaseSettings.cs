using MelloSilveiraTools.Core.ResiliencePipelines;

namespace MelloSilveiraTools.Database.RelationalDatabase.Settings;

/// <summary>
/// Settings that configure access to the application's relational database.
/// </summary>
public record DatabaseSettings
{
    /// <summary>
    /// Connection string used to reach the database server.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Timeout, in milliseconds, applied to single-row (unit) operations.
    /// </summary>
    public required int UnitOperationTimeoutInMilliseconds { get; init; }

    /// <summary>
    /// Timeout, in milliseconds, applied to bulk operations that touch many rows.
    /// </summary>
    public required int BulkOperationTimeoutInMilliseconds { get; init; }

    /// <summary>
    /// Optional resilience pipeline settings applied to database operations.
    /// </summary>
    public ResiliencePipelineSettings? ResiliencePipelineSettings { get; init; }

    internal int UnitOperationTimeoutInSeconds => UnitOperationTimeoutInMilliseconds / 1000;
    internal int BulkOperationTimeoutInSeconds => BulkOperationTimeoutInMilliseconds / 1000;
}
