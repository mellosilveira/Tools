namespace MelloSilveiraTools.Infrastructure.Database.Settings;

public record DatabaseSettings
{
    public string ConnectionString { get; init; }
    public int UnitOperationTimeoutInMilliseconds { get; init; }
    public int BulkOperationTimeoutInMilliseconds { get; init; }
}
