//using AdmMaster.DataContracts.Base;
//using AdmMaster.Integrations.Extensions;
//using Dasync.Collections;
//using MelloSilveiraTools.UseCases.Operations;
//using PraJah.Backend.Domain.Services.Base.ApiServiceAgent.DataContract;
//using PraJah.Backend.Domain.Services.Base.ApiServiceAgent.Settings;
//using PraJah.Backend.Domain.Services.Base.Retry;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace PraJah.Backend.Domain.Services.Base.ApiServiceAgent
//{
//    /// <inheritdoc cref="IApiServiceAgent"/>
//    public abstract class ApiServiceAgentBase : IApiServiceAgent
//    {
//        private bool _disposedValue;

//        /// <inheritdoc/>
//        public abstract string ServiceName { get; }

//        /// <summary>
//        /// Custom options to be used with <see cref="JsonSerializer"/>.
//        /// </summary>
//        protected JsonSerializerOptions JsonSerializerOptions { get; }

//        /// <summary>
//        /// HTTP client to be used on integration with API.
//        /// </summary>
//        protected HttpClient HttpClient { get; set; }

//        /// <summary>
//        /// Settings for integrations with an API.
//        /// </summary>
//        protected ApiServiceAgentSettings Settings { get; set; }

//        /// <summary>
//        /// Initializes a new instance of <see cref="ApiServiceAgentBase"/>.
//        /// </summary>
//        /// <param name="settings"></param>
//        protected ApiServiceAgentBase(ApiServiceAgentSettings settings)
//        {
//            JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
//            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

//            Settings = settings;

//            HttpClient = new HttpClient
//            {
//                BaseAddress = new Uri(settings.BaseAddress),
//                Timeout = TimeSpan.FromSeconds(settings.DefaultTimeoutInSeconds)
//            };
//        }

//        /// <summary>
//        /// Sends a POST request to the specified URI.
//        /// </summary>
//        /// <typeparam name="TRequest"></typeparam>
//        /// <typeparam name="TResponseData"></typeparam>
//        /// <param name="request"></param>
//        /// <param name="requestUri"></param>
//        /// <param name="timeoutInMiliseconds"></param>
//        /// <param name="methodName"></param>
//        /// <returns></returns>
//        protected Task<OperationResponseBase<TResponseData>> PostAsync<TRequest, TResponseData>(TRequest request, string requestUri, int timeoutInMiliseconds, string methodName)
//            where TRequest : OperationRequestBase
//            where TResponseData : class, new()
//        {
//            var cts = new CancellationTokenSource(timeoutInMiliseconds);
//            var body = new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions), Encoding.UTF8, "application/json");
//            return this.ExecuteAsync<TResponseData>(HttpClient.PostAsync(requestUri, body, cts.Token), methodName, cts.Token);
//        }

//        /// <summary>
//        /// Sends a GET request to the specified URI.
//        /// </summary>
//        /// <typeparam name="TResponseData"></typeparam>
//        /// <param name="requestUri"></param>
//        /// <param name="timeoutInMiliseconds"></param>
//        /// <param name="methodName"></param>
//        /// <param name="retryCount"></param>
//        /// <param name="retryIntervalInMiliseconds"></param>
//        /// <returns></returns>
//        protected Task<OperationResponseBase<TResponseData>> GetUnitAsync<TResponseData>(string requestUri, int timeoutInMiliseconds, string methodName, int? retryCount = null, int? retryIntervalInMiliseconds = null)
//            where TResponseData : class, new()
//        {
//            return RetryPolicy
//                .Get<OperationResponseBase<TResponseData>>(retryCount ?? Settings.DefaultRetryCount, retryIntervalInMiliseconds ?? Settings.DefaultRetryIntervalInMiliseconds)
//                .ExecuteAsync(() =>
//                {
//                    var cts = new CancellationTokenSource(timeoutInMiliseconds);
//                    return this.ExecuteAsync<TResponseData>(HttpClient.GetAsync(requestUri, cts.Token), methodName, cts.Token);
//                });
//        }

//        /// <summary>
//        /// Sends a GET request to the specified URI.
//        /// </summary>
//        /// <typeparam name="TResponseData"></typeparam>
//        /// <param name="requestUri"></param>
//        /// <param name="timeoutInMiliseconds"></param>
//        /// <param name="methodName"></param>
//        /// <param name="retryCount"></param>
//        /// <param name="retryIntervalInMiliseconds"></param>
//        /// <returns></returns>
//        protected Task<AsyncOperationResponse<TResponseData>> GetAsync<TResponseData>(string requestUri, int timeoutInMiliseconds, string methodName, int? retryCount = null, int? retryIntervalInMiliseconds = null)
//            where TResponseData : class
//        {
//            return RetryPolicy
//                .Get<AsyncOperationResponse<TResponseData>>(retryCount ?? Settings.DefaultRetryCount, retryIntervalInMiliseconds ?? Settings.DefaultRetryIntervalInMiliseconds)
//                .ExecuteAsync(async () =>
//                {
//                    var response = new AsyncOperationResponse<TResponseData>();
//                    var cts = new CancellationTokenSource(timeoutInMiliseconds);

//                    try
//                    {
//                        var result = await HttpClient.GetAsync(requestUri, cts.Token).ConfigureAwait(false);
//                        if (result.Content != null)
//                        {
//                            if (result.IsSuccessStatusCode)
//                            {
//                                var a = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
//                                using (var responseDataStream = await result.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false))
//                                {
//                                    // TODO: gambiarra para retornar um async enumerable, porque usando o "JsonSerializer.DeserializeAsyncEnumerable" não funcionou.
//                                    var responseDataAsList = await JsonSerializer.DeserializeAsync<List<TResponseData>>(responseDataStream, JsonSerializerOptions, cts.Token).ConfigureAwait(false);
//                                    response.Data = responseDataAsList.ToAsyncEnumerable();
//                                    response.SetSuccess(result.StatusCode);
//                                }
//                            }
//                            else
//                            {
//                                response.SetError(result.StatusCode, $"Failed on '{methodName.SafeRemove("Async")}'.");
//                            }
//                        }
//                    }
//                    catch (OperationCanceledException)
//                    {
//                        // TODO: logar exceção.
//                        response.SetRequestTimeoutError($"Timeout on integration with '{ServiceName}'.");
//                    }
//                    catch
//                    {
//                        // TODO: logar exceção.
//                        response.SetServiceUnavailableError($"Failed on integration with '{ServiceName}'.");
//                    }

//                    return response;
//                });
//        }

//        /// <summary>
//        /// Sends a DELETE request to the specified URI.
//        /// </summary>
//        /// <param name="requestUri"></param>
//        /// <param name="timeoutInMiliseconds"></param>
//        /// <param name="methodName"></param>
//        /// <returns></returns>
//        protected Task<OperationResponseBase> DeleteAsync(string requestUri, int timeoutInMiliseconds, string methodName)
//        {
//            var cts = new CancellationTokenSource(timeoutInMiliseconds);
//            return ExecuteAsync(HttpClient.DeleteAsync(requestUri, cts.Token), methodName);
//        }

//        /// <summary>
//        /// Sends an HTTP request to the specified URI.
//        /// </summary>
//        /// <typeparam name="TResponseData"></typeparam>
//        /// <param name="httpTask"></param>
//        /// <param name="cancellationToken"></param>
//        /// <param name="methodName"></param>
//        /// <returns></returns>
//        private async Task<OperationResponseBase<TResponseData>> ExecuteAsync<TResponseData>(Task<HttpResponseMessage> httpTask, string methodName, CancellationToken cancellationToken)
//            where TResponseData : OperationResponseData, new()
//        {
//            var response = new OperationResponseBase<TResponseData>();

//            try
//            {
//                var result = await httpTask.ConfigureAwait(false);
//                if (result.Content != null)
//                {
//                    if (result.IsSuccessStatusCode)
//                    {
//                        using (var responseDataStream = await result.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
//                        {
//                            response.Data = await JsonSerializer.DeserializeAsync<TResponseData>(responseDataStream, JsonSerializerOptions).ConfigureAwait(false);
//                            response.SetSuccess(result.StatusCode);
//                        }
//                    }
//                    else
//                    {
//                        // TODO: logar "errorContent"
//                        string errorContent = await result.Content.ReadAsStringAsync();
//                        response.SetError(result.StatusCode, $"Failed on '{methodName.SafeRemove("Async")}'.");
//                    }
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                // TODO: logar exceção.
//                // todo: trocar por GatewayTimeout
//                response.SetRequestTimeoutError($"Timeout on integration with '{ServiceName}'.");
//            }
//            catch
//            {
//                // TODO: logar exceção.
//                response.SetServiceUnavailableError($"Failed on integration with '{ServiceName}'.");
//            }

//            return response;
//        }

//        /// <summary>
//        /// Sends an HTTP request to the specified URI.
//        /// </summary>
//        /// <param name="httpTask"></param>
//        /// <param name="methodName"></param>
//        /// <returns></returns>
//        private async Task<OperationResponseBase> ExecuteAsync(Task<HttpResponseMessage> httpTask, string methodName)
//        {
//            var response = new OperationResponseBase();

//            try
//            {
//                var result = await httpTask.ConfigureAwait(false);
//                if (result.Content != null)
//                {
//                    if (result.IsSuccessStatusCode)
//                    {
//                        response.SetSuccess(result.StatusCode);
//                    }
//                    else
//                    {
//                        // TODO: logar "errorContent"
//                        string errorContent = await result.Content.ReadAsStringAsync();
//                        response.SetError(result.StatusCode, $"Failed on '{methodName.SafeRemove("Async")}'.");
//                    }
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                // TODO: logar exceção.
//                response.SetRequestTimeoutError($"Timeout on integration with '{ServiceName}'.");
//            }
//            catch
//            {
//                // TODO: logar exceção.
//                response.SetServiceUnavailableError($"Failed on integration with '{ServiceName}'.");
//            }

//            return response;
//        }

//        /// <summary>
//        /// See reference <see cref="Dispose()"/>.
//        /// </summary>
//        /// <param name="disposing">
//        /// Indicates whether the method call comes from a Dispose method (its value is true) or from a finalizer 
//        /// (its value is false).
//        /// </param>
//        protected virtual void Dispose(bool disposing)
//        {
//            if (_disposedValue == false)
//            {
//                // Dispose managed state (managed objects).
//                if (disposing)
//                {
//                    HttpClient.Dispose();
//                }

//                // Free unmanaged resources (unmanaged objects) and set large fields to null.
//                Settings = null;
//                _disposedValue = true;
//            }
//        }

//        /// <inheritdoc/>
//        public void Dispose()
//        {
//            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
//            Dispose(disposing: true);
//            GC.SuppressFinalize(this);
//        }

//        /// <summary>
//        /// Finalizes the current instance of <see cref="ApiServiceAgentBase"/>.
//        /// </summary>
//        ~ApiServiceAgentBase()
//        {
//            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
//            Dispose(disposing: false);
//        }
//    }
//}
