using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Models;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent;

/// <inheritdoc cref="IApiServiceAgent"/>
public abstract class ApiServiceAgentBase : IApiServiceAgent
{
    private bool _disposedValue;

    /// <inheritdoc/>
    public abstract string ServiceName { get; }

    /// <summary>
    /// Custom options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    protected static readonly JsonSerializerOptions JsonSerializerOptions;

    static ApiServiceAgentBase()
    {
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        options.MakeReadOnly();
        JsonSerializerOptions = options;
    }

    /// <inheritdoc cref="ILogger"/>
    protected ILogger Logger { get; }

    /// <summary>
    /// HTTP client to be used on integration with API.
    /// </summary>
    protected HttpClient HttpClient { get; }

    /// <summary>
    /// Settings for integrations with an API.
    /// </summary>
    protected ApiServiceAgentSettings Settings { get; }

    /// <inheritdoc cref="ApiServiceAgentResiliencePipeline"/>
    public ApiServiceAgentResiliencePipeline ResiliencePipeline { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ApiServiceAgentBase"/>.
    /// </summary>
    /// <param name="logger">Logger used to record execution details and errors.</param>
    /// <param name="settings">Settings controlling the connection with the external API.</param>
    /// <param name="resiliencePipeline">Resilience pipeline applied to outbound HTTP requests.</param>
    protected ApiServiceAgentBase(ILogger logger, ApiServiceAgentSettings settings, ApiServiceAgentResiliencePipeline resiliencePipeline)
    {
        Logger = logger;
        Settings = settings;
        ResiliencePipeline = resiliencePipeline;

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseAddress),
            Timeout = TimeSpan.FromSeconds(settings.DefaultTimeoutInSeconds)
        };
    }

    /// <summary>
    /// Sends a GET request to the specified URI and maps the JSON payload into a list-style operation response.
    /// </summary>
    /// <param name="requestUri">Relative or absolute URI of the endpoint to call.</param>
    /// <param name="timeoutInMiliseconds">Per-request timeout, in milliseconds.</param>
    /// <param name="methodName">Name of the caller method used to enrich log and error messages.</param>
    /// <returns>An operation response carrying the deserialized data or the failure reason.</returns>
    protected async Task<ListedResult<TResponseData>> GetAsync<TResponseData>(string requestUri, int timeoutInMiliseconds, [CallerMemberName] string methodName = "") where TResponseData : class => await ResiliencePipeline.ExecuteAsync(async _ =>
    {
        var token = new CancellationTokenSource(timeoutInMiliseconds).Token;
        try
        {
            HttpResponseMessage result = await HttpClient.GetAsync(requestUri, token).ConfigureAwait(false);
            if (result.Content != null)
            {
                string content = await result.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (result.IsSuccessStatusCode)
                {
                    var responseData = JsonSerializer.Deserialize<TResponseData[]>(content, JsonSerializerOptions);
                    return Result.CreateListedSuccess((StatusCode)result.StatusCode, responseData);
                }

                string cleanMethodName = methodName.Remove("Async");
                Logger.LogError("Failed on '{MethodName}'. Content: {Content}", cleanMethodName, content);
                return Result.CreateError((StatusCode)result.StatusCode, $"Failed on '{cleanMethodName}'.");
            }

            return Result.CreateUnknownError($"Failed on '{methodName.Remove("Async")}' due to null content.");
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogError(ex, "Timeout on integration with '{ServiceName}'.", ServiceName);
            return Result.CreateRequestTimeout($"Timeout on integration with '{ServiceName}'.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed on integration with '{ServiceName}'.", ServiceName);
            return Result.CreateServiceUnavailable($"Failed on integration with '{ServiceName}'.");
        }
    });

    /// <summary>
    /// Sends a GET request to the specified URI and streams the NDJSON response as an async sequence,
    /// yielding one deserialized record per line as data arrives.
    /// </summary>
    /// <typeparam name="T">The type of each record in the NDJSON stream.</typeparam>
    /// <param name="requestUri">Relative or absolute URI of the streaming endpoint to call.</param>
    /// <param name="timeoutInMilliseconds">Per-request timeout, in milliseconds. Applies to the entire stream.</param>
    /// <param name="methodName">Name of the caller method used to enrich log and error messages.</param>
    /// <param name="cancellationToken">Token that cancels the enumeration from the caller side.</param>
    /// <returns>
    /// An async sequence of deserialized <typeparamref name="T"/> records yielded as each NDJSON line arrives.
    /// Returns an empty sequence when the server responds with a non-success status code, a timeout occurs,
    /// or a connection error is raised before the first line.
    /// </returns>
    /// <example>
    /// <code>
    /// await foreach (var item in GetStreamAsync&lt;MyRecord&gt;("/api/stream", 30_000, nameof(MyMethodAsync)))
    ///     Process(item);
    /// </code>
    /// </example>
    protected async IAsyncEnumerable<T> GetStreamAsync<T>(
        string requestUri,
        int timeoutInMilliseconds,
        string methodName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : class
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutInMilliseconds);

        (HttpResponseMessage Response, StreamReader Reader)? connection = await OpenNdjsonStreamAsync(requestUri, methodName, cts.Token).ConfigureAwait(false);
        if (connection is null)
            yield break;

        HttpResponseMessage response = connection.Value.Response;
        StreamReader reader = connection.Value.Reader;

        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cts.Token).ConfigureAwait(false)) is not null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                T? item = JsonSerializer.Deserialize<T>(line, JsonSerializerOptions);
                if (item is not null)
                    yield return item;
            }

            // After the body is fully consumed the HTTP stack populates TrailingHeaders.
            // Verify the server committed the full stream successfully.
            bool streamSucceeded = response.TrailingHeaders.TryGetValues(ApplicationConstants.StreamStatusTrailerName, out IEnumerable<string>? trailerValues) && trailerValues.FirstOrDefault() == ApplicationConstants.StreamSuccessfullyStatus;
            if (!streamSucceeded)
            {
                Logger.LogError(
                    "Stream from '{ServiceName}' did not complete successfully — trailer '{TrailerName}' was not received.",
                    ServiceName,
                    ApplicationConstants.StreamStatusTrailerName);
            }
        }
        finally
        {
            reader.Dispose();
            response.Dispose();
        }
    }

    protected async Task<Result> ExecuteAsync(Task<HttpResponseMessage> httpTask, string methodName, CancellationToken cancellationToken)
    {
        try
        {
            var result = await httpTask.ConfigureAwait(false);
            if (result.Content != null)
            {
                if (result.IsSuccessStatusCode)
                    return Result.CreateSuccess((StatusCode)result.StatusCode);

                string content = await result.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                string cleanMethodName = methodName.Remove("Async");
                Logger.LogError("Failed on '{MethodName}'. Content: {Content}", cleanMethodName, content);
                return Result.CreateError((StatusCode)result.StatusCode, $"Failed on '{cleanMethodName}'.");
            }

            return Result.CreateUnknownError($"Failed on '{methodName.Remove("Async")}' due to null content.");
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogError(ex, "Timeout on integration with '{ServiceName}'.", ServiceName);
            return Result.CreateRequestTimeout($"Timeout on integration with '{ServiceName}'.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed on integration with '{ServiceName}'.", ServiceName);
            return Result.CreateServiceUnavailable($"Failed on integration with '{ServiceName}'.");
        }
    }

    private async Task<(HttpResponseMessage Response, StreamReader Reader)?> OpenNdjsonStreamAsync(string requestUri, string methodName, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await HttpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                Logger.LogError("Failed on '{MethodName}'. Content: {Content}", methodName.Remove("Async"), content);
                response.Dispose();
                return null;
            }

            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return (response, new StreamReader(stream));
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogError(ex, "Timeout on integration with '{ServiceName}'.", ServiceName);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed on integration with '{ServiceName}'.", ServiceName);
            return null;
        }
    }

    /// <summary>
    /// See reference <see cref="Dispose()"/>.
    /// </summary>
    /// <param name="disposing">
    /// Indicates whether the method call comes from a Dispose method (its value is true) or from a finalizer 
    /// (its value is false).
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue == false)
        {
            // Dispose managed state (managed objects).
            if (disposing)
            {
                HttpClient.Dispose();
            }

            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}