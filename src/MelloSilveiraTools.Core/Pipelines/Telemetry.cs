using System.Diagnostics;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Exposes the centralized ActivitySource for tracing pipeline execution topologies.
/// </summary>
public static class Telemetry
{
    public const string PipelineSourceName = "MelloSilveiraTools";

    public static readonly ActivitySource DefaultInstance = new(PipelineSourceName, "1.0.0");
}
