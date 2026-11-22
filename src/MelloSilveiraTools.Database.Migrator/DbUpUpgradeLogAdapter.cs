using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MelloSilveiraTools.Database.Migrator;

internal sealed class DbUpUpgradeLogAdapter(ILogger logger) : IUpgradeLog
{
    public void LogTrace(string format, params object[] args) =>
        logger.LogTrace("{Message}", FormatMessage(format, args));

    public void LogDebug(string format, params object[] args) =>
        logger.LogDebug("{Message}", FormatMessage(format, args));

    public void LogInformation(string format, params object[] args) =>
        logger.LogInformation("{Message}", FormatMessage(format, args));

    public void LogWarning(string format, params object[] args) =>
        logger.LogWarning("{Message}", FormatMessage(format, args));

    public void LogError(string format, params object[] args) =>
        logger.LogError("{Message}", FormatMessage(format, args));

    public void LogError(Exception ex, string format, params object[] args) =>
        logger.LogError(ex, "{Message}", FormatMessage(format, args));

    private static string FormatMessage(string format, object[] args) =>
        args.Length == 0 ? format : string.Format(CultureInfo.InvariantCulture, format, args);
}
