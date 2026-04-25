namespace MelloSilveiraTools.Infrastructure.Database.Settings;

/// <summary>
/// Settings that configure access to the application's relational database.
/// </summary>
public record DatabaseSettings
{
    /// <summary>
    /// Connection string used to reach the database server.
    /// </summary>
    public string ConnectionString { get; init; }

    /// <summary>
    /// Timeout, in milliseconds, applied to single-row (unit) operations.
    /// </summary>
    public int UnitOperationTimeoutInMilliseconds { get; init; }

    /// <summary>
    /// Timeout, in milliseconds, applied to bulk operations that touch many rows.
    /// </summary>
    public int BulkOperationTimeoutInMilliseconds { get; init; }

    internal int UnitOperationTimeoutInSeconds => UnitOperationTimeoutInMilliseconds / 1000;
    internal int BulkOperationTimeoutInSeconds => BulkOperationTimeoutInMilliseconds / 1000;
}
