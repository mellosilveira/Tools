using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Provides real-time NDJSON (<c>application/x-ndjson</c>) streaming of <see cref="PipelineProgressEvent"/> notifications
/// over an active <see cref="HttpResponse"/> while an asynchronous pipeline command executes.
/// </summary>
// TODO: DEVE SER AGNÓSTICO A IPipelineProgressNotifier E FUNCIONAR PARA QUALQUER NOTIFICAÇÃO
internal static class PipelineProgressStreamWriter
{
    private static readonly byte[] Newline = [(byte)'\n'];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Executes the supplied pipeline operation while streaming all emitted <see cref="PipelineProgressEvent"/> notifications
    /// in real time as newline-delimited JSON (NDJSON) to the HTTP response body.
    /// </summary>
    /// <typeparam name="TResponse">The response payload type wrapped in the final <see cref="Result{T}"/>.</typeparam>
    /// <param name="analysisIdentifier">Mandatory business identifier correlating the analysis.</param>
    /// <param name="progressNotifier">The progress notifier resolved via dependency injection.</param>
    /// <param name="timeProvider">Mandatory time provider for timestamping execution events.</param>
    /// <param name="httpContext">The active HTTP context.</param>
    /// <param name="executeAsync">Delegate that executes the command.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    public static async Task StreamProgressAsync<TResponse>(
        string analysisIdentifier,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        HttpContext httpContext,
        Func<CancellationToken, Task<Result<TResponse>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisIdentifier);

        HttpResponse response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "application/x-ndjson; charset=utf-8";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        ChannelPipelineProgressNotifier? channelNotifier = progressNotifier as ChannelPipelineProgressNotifier;

        Task<Result<TResponse>> executionTask = Task.Run(async () =>
        {
            try
            {
                Result<TResponse> result = await executeAsync(cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    await progressNotifier.NotifyAsync(
                        analysisIdentifier,
                        new PipelineProgressEvent
                        {
                            AnalysisIdentifier = analysisIdentifier,
                            StepName = "Command",
                            Description = string.Join("; ", result.Messages),
                            State = PipelineStepState.Faulted,
                            Timestamp = timeProvider.GetUtcNow()
                        },
                        cancellationToken).ConfigureAwait(false);
                }

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await progressNotifier.NotifyAsync(
                    analysisIdentifier,
                    new PipelineProgressEvent
                    {
                        AnalysisIdentifier = analysisIdentifier,
                        StepName = "Command",
                        Description = ex.Message,
                        State = PipelineStepState.Faulted,
                        Timestamp = timeProvider.GetUtcNow(),
                        Details = ex.GetType().Name
                    },
                    cancellationToken).ConfigureAwait(false);
                throw;
            }
            finally
            {
                channelNotifier?.TryComplete(analysisIdentifier);
            }
        }, cancellationToken);

        if (channelNotifier is not null)
        {
            await foreach (PipelineProgressEvent progressEvent in channelNotifier.ReadAllAsync(analysisIdentifier, cancellationToken).ConfigureAwait(false))
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(progressEvent, JsonOptions);
                await response.Body.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await response.Body.WriteAsync(Newline, cancellationToken).ConfigureAwait(false);
                await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await executionTask.ConfigureAwait(false);
    }
}
