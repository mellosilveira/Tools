using Serilog;

namespace MelloSilveiraTools.Core.Logging;

public static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration Create() => new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName();

    extension(LoggerConfiguration loggerConfiguration)
    {
        public LoggerConfiguration WriteToLocalFile(LoggerSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Directory))
                return loggerConfiguration;

            if (!Directory.Exists(settings.Directory))
                Directory.CreateDirectory(settings.Directory);

            string pathFormat = Path.Combine(settings.Directory, $"{settings.FileNamePrefix}-.json");
            return loggerConfiguration.WriteTo.File(
                path: pathFormat,
                rollingInterval: settings.RollDaily ? RollingInterval.Day : RollingInterval.Infinite,
                fileSizeLimitBytes: settings.MaxFileSizeBytes,
                retainedFileCountLimit: settings.MaxRetainedFiles > 0 ? settings.MaxRetainedFiles : null,
                shared: true);
        }
    }
}
