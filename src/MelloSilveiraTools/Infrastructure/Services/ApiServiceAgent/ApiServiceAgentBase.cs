using MelloSilveiraTools.Application.Operations;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.Infrastructure.Services.ApiServiceAgent.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Infrastructure.Services.ApiServiceAgent;

/// <inheritdoc cref="IApiServiceAgent"/>
public abstract class ApiServiceAgentBase : IApiServiceAgent
{
    private bool _disposedValue;

    /// <inheritdoc/>
    public abstract string ServiceName { get; }

    /// <summary>
    /// Custom options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    protected static JsonSerializerOptions JsonSerializerOptions
    {
        get
        {
            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
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
    /// <param name="logger"></param>
    /// <param name="settings"></param>
    /// <param name="resiliencePipeline"></param>
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
    /// Sends a GET request to the specified URI.
    /// </summary>
    /// <param name="requestUri"></param>
    /// <param name="timeoutInMiliseconds"></param>
    /// <param name="methodName"></param>
    /// <returns></returns>
    protected async Task<TResponse> GetAsync<TResponse, TResponseData>(string requestUri, int timeoutInMiliseconds, string methodName)
        where TResponse : OperationListResponseBase<TResponseData>, new()
        where TResponseData : class, new()
    {
        return await ResiliencePipeline.ExecuteAsync(async _ =>
        {
            CancellationTokenSource cts = new(timeoutInMiliseconds);
            return await ExecuteAsync<TResponse, TResponseData>(HttpClient.GetAsync(requestUri, cts.Token), methodName, cts.Token);
        });
    }

    private async Task<TResponse> ExecuteAsync<TResponse, TResponseData>(Task<HttpResponseMessage> httpTask, string methodName, CancellationToken cancellationToken)
        where TResponse : OperationListResponseBase<TResponseData>, new()
        where TResponseData : class, new()
    {
        try
        {
            HttpResponseMessage result = await httpTask.ConfigureAwait(false);
            if (result.Content != null)
            {
                string content = await result.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsSuccessStatusCode)
                {
                    TResponseData[]? responseData = JsonSerializer.Deserialize<TResponseData[]>(content, JsonSerializerOptions);
                    return OperationResponse.CreateListSuccess<TResponse, TResponseData>(result.StatusCode, responseData);
                }

                string message = $"Failed on '{methodName.Remove("Async")}'.";

                Dictionary<string, object?> logAdditionalData = new() { { "Content", content } };
                Logger.Error(message, null, logAdditionalData);

                return OperationResponse.CreateError<TResponse>(result.StatusCode, message);
            }

            return OperationResponse.CreateInternalServerError<TResponse>($"Failed on '{methodName.Remove("Async")}' due to null content.");
        }
        catch (OperationCanceledException ex)
        {
            string message = $"Timeout on integration with '{ServiceName}'.";
            Logger.Error(message, ex);

            return OperationResponse.CreateRequestTimeout<TResponse>(message);
        }
        catch (Exception ex)
        {
            string message = $"Failed on integration with '{ServiceName}'.";
            Logger.Error(message, ex);

            return OperationResponse.CreateServiceUnavailable<TResponse>(message);
        }
    }

    private async Task<OperationResponse> ExecuteAsync(Task<HttpResponseMessage> httpTask, string methodName)
    {
        try
        {
            var result = await httpTask.ConfigureAwait(false);
            if (result.Content != null)
            {
                if (result.IsSuccessStatusCode)
                    return OperationResponse.CreateSuccess(result.StatusCode);

                string message = $"Failed on '{methodName.Remove("Async")}'.";
                string content = await result.Content.ReadAsStringAsync();

                var logAdditionalData = new Dictionary<string, object?> { { "Content", content } };

                Logger.Error(message, null, logAdditionalData);

                return OperationResponse.CreateError(result.StatusCode, message);
            }

            return OperationResponse.CreateInternalServerError($"Failed on '{methodName.Remove("Async")}' due to null content.");
        }
        catch (OperationCanceledException ex)
        {
            string message = $"Timeout on integration with '{ServiceName}'.";
            Logger.Error(message, ex);

            return OperationResponse.CreateRequestTimeout(message);
        }
        catch (Exception ex)
        {
            string message = $"Failed on integration with '{ServiceName}'.";
            Logger.Error(message, ex);

            return OperationResponse.CreateServiceUnavailable(message);
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

    /// <summary>
    /// Finalizes the current instance of <see cref="ApiServiceAgentBase"/>.
    /// </summary>
    ~ApiServiceAgentBase()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
        Dispose(disposing: false);
    }
}
