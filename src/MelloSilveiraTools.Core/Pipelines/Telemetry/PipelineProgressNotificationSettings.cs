namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

/// <summary>
/// Configuration settings that define adaptive throttling thresholds and item frequency intervals 
/// for real-time pipeline progress notifications.
/// </summary>
public sealed record PipelineProgressNotificationSettings
{
    /// <summary>
    /// Default threshold limit up to which every individual item processed emits a progress notification event.
    /// </summary>
    public const int DefaultImmediateNotificationItemLimit = 50;

    /// <summary>
    /// Default item interval frequency for emitting progress notification events once the processed volume exceeds <see cref="DefaultImmediateNotificationItemLimit"/>.
    /// </summary>
    public const int DefaultHighVolumeNotificationInterval = 500;

    /// <summary>
    /// Gets the upper limit of items for which every processed item emits an individual progress event.
    /// Must be greater than zero.
    /// </summary>
    public required int ImmediateNotificationItemLimit { get; init; }

    /// <summary>
    /// Gets the item frequency interval for emitting progress events once the volume exceeds <see cref="ImmediateNotificationItemLimit"/>.
    /// Must be greater than zero.
    /// </summary>
    public required int HighVolumeNotificationInterval { get; init; }
}
