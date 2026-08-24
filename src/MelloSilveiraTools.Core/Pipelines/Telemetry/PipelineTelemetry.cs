using System.Diagnostics;

namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

/// <summary>
/// Exposes the centralized ActivitySource for tracing pipeline execution topologies.
/// </summary>
public static class PipelineTelemetry
{
    public const string SourceName = "MelloSilveiraTools.Core.Pipelines";

    public static readonly ActivitySource Instance = new(SourceName, "1.0.0");
}
