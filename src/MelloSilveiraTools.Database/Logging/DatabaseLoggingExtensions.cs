using MelloSilveiraTools.Core.Logging;
using Serilog;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace MelloSilveiraTools.Database.Logging;

public static class DatabaseLoggingExtensions
{
    extension(LoggerConfiguration loggerConfiguration)
    {
        /// <summary>
        /// Adds the PostgreSQL batch-insert sink to the Serilog configuration using the unified settings.
        /// </summary>
        public LoggerConfiguration WriteToPostgres(LoggerSettings settings)
            => string.IsNullOrWhiteSpace(settings.PostgreSqlConnectionString)
                ? loggerConfiguration
                : loggerConfiguration.WriteTo.PostgreSQL(
                    connectionString: settings.PostgreSqlConnectionString,
                    tableName: settings.TableName!,
                    schemaName: settings.SchemaName ?? "public",
                    needAutoCreateTable: false, // Table managed by your migrations
                    batchSizeLimit: settings.BatchSizeLimit,
                    period: TimeSpan.FromSeconds(5), // Flush every 5 seconds if batch isn't full
                    columnOptions: new Dictionary<string, ColumnWriterBase>
                    {
                        { "message", new RenderedMessageColumnWriter() },
                        { "message_template", new MessageTemplateColumnWriter() },
                        { "level", new LevelColumnWriter() },
                        { "time_stamp", new TimestampColumnWriter() },
                        { "exception", new ExceptionColumnWriter() },
                        { "properties", new LogEventSerializedColumnWriter() } // JSONB mapped.
                    }
                );
    }
}