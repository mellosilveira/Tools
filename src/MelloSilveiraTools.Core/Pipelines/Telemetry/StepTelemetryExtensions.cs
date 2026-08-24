using System.Diagnostics;

namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

/// <summary>
/// Provides tracing instrumentation extensions for pipeline step execution.
/// </summary>
internal static class StepTelemetryExtensions
{
    /// <summary>
    /// Wraps a step execution delegate within an OpenTelemetry span, automatically 
    /// tracking latency, status codes, and capturing unhandled exceptions.
    /// </summary>
    public static async Task<TOut> ExecuteWithTracingAsync<TIn, TOut>(
        string stepName,
        TIn input,
        Func<TIn, CancellationToken, Task<TOut>> executionCore,
        CancellationToken cancellationToken)
    {
        // Start an internal OpenTelemetry span for the step
        using var activity = PipelineTelemetry.Instance.StartActivity($"Pipeline.Step.{stepName}", ActivityKind.Internal);

        activity?.SetTag("pipeline.step.name", stepName);

        try
        {
            var result = await executionCore(input, cancellationToken).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            // Attach the exception and stack trace to the telemetry span using native API
            activity?.AddException(ex);

            throw;
        }
    }
}