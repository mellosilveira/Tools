namespace MelloSilveiraTools.Infrastructure.Database.Settings;

public record DatabaseSettings
{
    public string ConnectionString { get; init; }
    public int UnitOperationTimeoutInMilliseconds { get; init; }
    public int BulkOperationTimeoutInMilliseconds { get; init; }

    internal int UnitOperationTimeoutInSeconds => UnitOperationTimeoutInMilliseconds / 1000;
    internal int BulkOperationTimeoutInSeconds => BulkOperationTimeoutInMilliseconds / 1000;
}
