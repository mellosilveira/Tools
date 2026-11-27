using Serilog;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace MelloSilveiraTools.Core.Logging;

/// <summary>
/// Provides extension methods for configuring Serilog sinks from strongly typed settings.
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Creates a base Serilog configuration configured with default enrichers and minimum level.
    /// </summary>
    /// <returns>A new <see cref="LoggerConfiguration"/> instance.</returns>
    public static LoggerConfiguration Create() => new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName();

    extension(LoggerConfiguration loggerConfiguration)
    {
        /// <summary>
        /// Adds a rolling local JSON file sink to the Serilog configuration using the file logger settings.
        /// </summary>
        /// <param name="settings">The file logging settings.</param>
        /// <returns>The modified <see cref="LoggerConfiguration"/>.</returns>
        public LoggerConfiguration WriteToLocalFile(FileLoggerSettings settings)
        {
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

        /// <summary>
        /// Adds the PostgreSQL batch-insert sink to the Serilog configuration using the database logger settings.
        /// </summary>
        /// <param name="settings">The database logger settings.</param>
        /// <returns>The modified <see cref="LoggerConfiguration"/>.</returns>
        public LoggerConfiguration WriteToPostgres(DatabaseLoggerSettings settings) => loggerConfiguration.WriteTo.PostgreSQL(
            connectionString: settings.ConnectionString,
            tableName: settings.TableName,
            schemaName: settings.SchemaName ?? "public",
            needAutoCreateTable: settings.NeedAutoCreateTable ?? false,
            batchSizeLimit: settings.BatchSizeLimit ?? 30,
            period: settings.Period ?? TimeSpan.FromSeconds(5),
            columnOptions: new Dictionary<string, ColumnWriterBase>
            {
                { "message", new RenderedMessageColumnWriter() },
                { "message_template", new MessageTemplateColumnWriter() },
                { "level", new LevelColumnWriter() },
                { "time_stamp", new TimestampColumnWriter() },
                { "exception", new ExceptionColumnWriter() },
                { "properties", new LogEventSerializedColumnWriter() }
            });
    }
}
