using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Channels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Provides real-time NDJSON (<c>application/x-ndjson</c>) streaming of <see cref="PipelineProgressEvent"/> notifications
/// over an active <see cref="HttpResponse"/> while an asynchronous pipeline command executes.
/// </summary>
internal static class PipelineProgressStreamWriter
{
    // TODO: REVISAR

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
    /// <param name="httpContext">The active HTTP context.</param>
    /// <param name="executeAsync">Delegate that accepts an <see cref="IProgress{T}"/> instance and executes the command.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    public static async Task StreamProgressAsync<TResponse>(
        HttpContext httpContext,
        Func<IProgress<PipelineProgressEvent>, CancellationToken, Task<Result<TResponse>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        HttpResponse response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "application/x-ndjson; charset=utf-8";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        Channel<PipelineProgressEvent> channel = Channel.CreateUnbounded<PipelineProgressEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        ChannelProgressReporter reporter = new(channel.Writer);

        Task<Result<TResponse>> executionTask = Task.Run(async () =>
        {
            try
            {
                Result<TResponse> result = await executeAsync(reporter, cancellationToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    reporter.Report(new PipelineProgressEvent(
                        "Command",
                        string.Join("; ", result.Messages),
                        PipelineStepState.Faulted,
                        Timestamp: DateTimeOffset.UtcNow));
                }

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                reporter.Report(new PipelineProgressEvent(
                    "Command",
                    ex.Message,
                    PipelineStepState.Faulted,
                    Detail: ex.GetType().Name,
                    Timestamp: DateTimeOffset.UtcNow));
                throw;
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, cancellationToken);

        await foreach (PipelineProgressEvent progressEvent in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(progressEvent, JsonOptions);
            await response.Body.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await response.Body.WriteAsync(Newline, cancellationToken).ConfigureAwait(false);
            await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        await executionTask.ConfigureAwait(false);
    }

    private sealed class ChannelProgressReporter(ChannelWriter<PipelineProgressEvent> writer) : IProgress<PipelineProgressEvent>
    {
        public void Report(PipelineProgressEvent value)
        {
            writer.TryWrite(value);
        }
    }
}
