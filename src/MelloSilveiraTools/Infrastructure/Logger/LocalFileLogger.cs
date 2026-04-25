using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Infrastructure.Logger;

/// <summary>
/// <see cref="ILogger"/> implementation that appends one JSON-encoded entry per line to a local file,
/// rolling the file by UTC day and/or by size.
/// </summary>
/// <remarks>
/// <para>
/// Thread-safe via an internal lock. Suitable for low-to-medium throughput. For high-throughput production
/// workloads, prefer a logger backed by an asynchronous queue or a structured logging framework
/// (Serilog, NLog, OpenTelemetry).
/// </para>
/// <para>Each entry has the shape (newlines added for readability — actual output is a single line):</para>
/// <code>
/// {
///   "timestamp": "2026-04-25T10:30:15.1234567Z",
///   "level": "Error",
///   "tags": ["MyController", "DoSomething"],
///   "message": "Something failed.",
///   "exception": { "type": "...", "message": "...", "stackTrace": "...", "inner": { ... } },
///   "data": { "userId": 42, "correlationId": "..." }
/// }
/// </code>
/// <para>Rotation:</para>
/// <list type="bullet">
///   <item>If <see cref="LoggerSettings.RollDaily"/> is <c>true</c>, a new file is opened on the first write of each UTC day with name <c>{FileNamePrefix}-{yyyy-MM-dd}.log</c>.</item>
///   <item>If <see cref="LoggerSettings.MaxFileSizeBytes"/> is set, the file is rolled to a time-suffixed copy when the size threshold is reached.</item>
///   <item><see cref="LoggerSettings.MaxRetainedFiles"/> controls how many files are kept in the directory; older files are deleted on roll.</item>
/// </list>
/// </remarks>
public sealed class LocalFileLogger : LoggerBase, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly LoggerSettings _settings;
    private readonly Lock _writeLock = new();
    private DateOnly _currentDate;
    private FileStream? _stream;
    private StreamWriter? _writer;
    private bool _disposed;

    /// <summary>
    /// Creates a new logger and opens the file for the current UTC day. The target directory is created if missing.
    /// </summary>
    /// <param name="settings">Logger configuration. Use <see cref="LoggerSettings"/> defaults for a quick start.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the process cannot create the directory or open the file.</exception>
    /// <exception cref="IOException">Thrown when the directory cannot be created or the file cannot be opened.</exception>
    public LocalFileLogger(LoggerSettings settings)
    {
        _settings = settings;
        Directory.CreateDirectory(settings.Directory);
        OpenFile(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <inheritdoc/>
    protected override void WriteLog(string message, LogLevel logLevel, Exception? ex = null, IList<string>? tags = null, IDictionary<string, object?>? additionalData = null)
    {
        DateTime now = DateTime.UtcNow;

        LogEntry entry = new()
        {
            Timestamp = now.ToString("o"),
            Level = logLevel.ToString(),
            Tags = tags?.Count > 0 ? tags : null,
            Message = message,
            Exception = ex is null ? null : LogException.From(ex),
            Data = additionalData?.Count > 0 ? additionalData : null,
        };

        string line = JsonSerializer.Serialize(entry, JsonOptions);

        lock (_writeLock)
        {
            if (_disposed)
            {
                return;
            }

            DateOnly today = DateOnly.FromDateTime(now);
            bool needsDailyRoll = _settings.RollDaily && today != _currentDate;
            bool needsSizeRoll = !needsDailyRoll
                && _settings.MaxFileSizeBytes is long maxBytes
                && _stream is not null
                && _stream.Length + line.Length + Environment.NewLine.Length >= maxBytes;

            if (needsDailyRoll)
            {
                CloseFile();
                OpenFile(today);
                CleanupOldFiles();
            }
            else if (needsSizeRoll)
            {
                CloseFile();
                OpenFile(today, rolledBySize: true);
                CleanupOldFiles();
            }

            _writer!.WriteLine(line);
            _writer.Flush();
        }
    }

    /// <summary>
    /// Flushes and releases the underlying file. Subsequent writes are silently dropped.
    /// </summary>
    public void Dispose()
    {
        lock (_writeLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CloseFile();
        }
    }

    private void OpenFile(DateOnly date, bool rolledBySize = false)
    {
        _currentDate = date;

        string baseName = _settings.RollDaily
            ? $"{_settings.FileNamePrefix}-{date:yyyy-MM-dd}"
            : _settings.FileNamePrefix;
        string fileName = rolledBySize
            ? $"{baseName}-{DateTime.UtcNow:HHmmssfff}.log"
            : $"{baseName}.log";

        string filePath = Path.Combine(_settings.Directory, fileName);

        _stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private void CloseFile()
    {
        try
        {
            _writer?.Flush();
        }
        catch
        {
            // Best-effort flush; the file is being closed regardless.
        }

        _writer?.Dispose();
        _stream?.Dispose();
        _writer = null;
        _stream = null;
    }

    private void CleanupOldFiles()
    {
        if (_settings.MaxRetainedFiles <= 0)
        {
            return;
        }

        try
        {
            FileInfo[] files = new DirectoryInfo(_settings.Directory).GetFiles($"{_settings.FileNamePrefix}*.log");
            foreach (FileInfo file in files.OrderByDescending(f => f.LastWriteTimeUtc).Skip(_settings.MaxRetainedFiles))
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    // Best-effort delete: a concurrent process may have a handle on the file.
                }
            }
        }
        catch
        {
            // Best-effort cleanup: directory enumeration must never bring down the logger.
        }
    }

    private sealed record LogEntry
    {
        [JsonPropertyName("timestamp")] public string Timestamp { get; init; } = string.Empty;
        [JsonPropertyName("level")] public string Level { get; init; } = string.Empty;
        [JsonPropertyName("tags")] public IList<string>? Tags { get; init; }
        [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
        [JsonPropertyName("exception")] public LogException? Exception { get; init; }
        [JsonPropertyName("data")] public IDictionary<string, object?>? Data { get; init; }
    }

    private sealed record LogException
    {
        [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
        [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
        [JsonPropertyName("stackTrace")] public string? StackTrace { get; init; }
        [JsonPropertyName("inner")] public LogException? Inner { get; init; }

        public static LogException From(Exception ex) => new()
        {
            Type = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            Inner = ex.InnerException is null ? null : From(ex.InnerException),
        };
    }
}
