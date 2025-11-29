namespace MelloSilveiraTools.Infrastructure.Database.Settings;

public record DatabaseSettings
{
    public string ConnectionString { get; init; }

    public int ConnectionTimeoutInMilliseconds { get; init; }

    public int UnitOperationTimeoutInMilliseconds { get; init; }
}
