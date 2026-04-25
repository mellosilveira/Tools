namespace MelloSilveiraTools.Infrastructure.Logger;

/// <summary>
/// Configuration for <see cref="LocalFileLogger"/>: target directory, file naming, rotation policy and retention.
/// </summary>
/// <remarks>
/// Register a configured instance with <c>services.AddSingleton(new LoggerSettings { ... })</c> before calling
/// <c>AddToolsServices</c>; otherwise <c>AddToolsServices</c> registers an instance with default values via
/// <see cref="Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </remarks>
public record LoggerSettings
{
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
    /// <c>{FileNamePrefix}-yyyy-MM-dd.log</c>. Defaults to <c>true</c>.
    /// </summary>
    public bool RollDaily { get; init; } = true;

    /// <summary>
    /// Maximum size, in bytes, after which the active file is rolled to a new suffixed file
    /// (<c>{FileNamePrefix}-yyyy-MM-dd-HHmmssfff.log</c>). Set to <c>null</c> to disable size-based rolling.
    /// Defaults to 10&#160;MB.
    /// </summary>
    public long? MaxFileSizeBytes { get; init; } = 10L * 1024 * 1024;

    /// <summary>
    /// Maximum number of log files to retain in <see cref="Directory"/>. Older files are deleted on every roll.
    /// Set to <c>0</c> or negative to disable retention cleanup. Defaults to 30.
    /// </summary>
    public int MaxRetainedFiles { get; init; } = 30;
}
