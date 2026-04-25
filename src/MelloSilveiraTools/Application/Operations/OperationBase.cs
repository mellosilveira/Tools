using MelloSilveiraTools.Infrastructure.Logger;
using System.Net;

namespace MelloSilveiraTools.Application.Operations;

/// <summary>
/// Represents the base for all operations in the application.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponse">Response type produced by the operation.</typeparam>
/// <param name="logger">Logger used to record failures raised while processing the operation.</param>
public abstract class OperationBase<TRequest, TResponse>(ILogger logger)
    where TRequest : OperationRequestBase, new()
    where TResponse : OperationResponse, new()
{
    /// <summary>
    /// Logger used to report failures raised while processing the operation.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// The main method of all operations.
    /// Asynchronously, orchestrates and validates the operations.
    /// </summary>
    /// <param name="request">The operation request content.</param>
    /// <returns>The operation response.</returns>
    /// <example>
    /// <code>
    /// var operation = serviceProvider.GetRequiredService&lt;CreateUserOperation&gt;();
    /// var response = await operation.ProcessAsync(new CreateUserRequest { Email = "user@example.com" });
    /// if (!response.Success)
    ///     return BadRequest(response.ErrorMessages);
    /// return StatusCode((int)response.StatusCode);
    /// </code>
    /// </example>
    public async Task<TResponse> ProcessAsync(TRequest request)
    {
        try
        {
            var validateResponse = await ValidateOperationAsync(request).ConfigureAwait(false);
            if (!validateResponse.Success)
                return validateResponse;

            return await ProcessOperationAsync(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
#if DEBUG
            string message = $"{ex}";
#else
            string message = "Ocorreu um erro interno durante o processamento da solicitação.";
#endif

            Dictionary<string, object?> logAdditionalData = new() { { "Request", request } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError<TResponse>(message);
        }
    }

    /// <summary>
    /// Asynchronously executes the use-case domain logic for this operation. Implementations should
    /// rely on <see cref="Logger"/> (inherited from this base class) for diagnostic logging and
    /// build their response using the <see cref="OperationResponse"/> factory helpers
    /// (<c>CreateSuccessOk</c>, <c>CreateNotFound</c>, <c>CreateInternalServerError</c>, etc.) for
    /// expected outcomes. Unexpected exceptions should be allowed to propagate — they are caught by
    /// <see cref="ProcessAsync"/> and translated into a 500 Internal Server Error response.
    /// </summary>
    /// <param name="request">The validated request payload. <see cref="ValidateOperationAsync"/> has already returned a successful result by the time this method runs.</param>
    /// <returns>The operation response, populated with the outcome of the domain logic.</returns>
    protected abstract Task<TResponse> ProcessOperationAsync(TRequest request);

    /// <summary>
    /// Asynchronously validates the inbound <paramref name="request"/> before
    /// <see cref="ProcessOperationAsync"/> runs. Contract: return a response with
    /// <see cref="OperationResponse.Success"/> set to <see langword="false"/> (typically built via
    /// <c>CreateError</c>/<c>CreateNotFound</c>/<c>CreateUnauthorized</c>) to abort the pipeline and
    /// short-circuit the response back to the caller; return a successful response (or one whose
    /// <c>Success</c> property is <see langword="true"/>) to allow <see cref="ProcessOperationAsync"/>
    /// to execute.
    /// </summary>
    /// <param name="request">The raw request payload received from the caller.</param>
    /// <returns>A response carrying the validation outcome.</returns>
    protected abstract Task<TResponse> ValidateOperationAsync(TRequest request);
}

/// <summary>
/// Base class for operations that return an <see cref="OperationResponseBase{TResponseData}"/> carrying a single data payload.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponseData">Type of the data payload returned by the operation.</typeparam>
public abstract class OperationBaseWithData<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, OperationResponseBase<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class
{
    /// <summary>
    /// Builds a successful response with the supplied status code and optional data.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Optional data payload.</param>
    protected OperationResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData? data = null)
        => new() { StatusCode = statusCode, Data = data, Success = true };

    /// <summary>
    /// Builds a successful 200 OK response with optional data.
    /// </summary>
    /// <param name="data">Optional data payload.</param>
    protected OperationResponseBase<TResponseData> CreateSuccessOk(TResponseData? data = null) => CreateSuccess(HttpStatusCode.OK, data);

    /// <summary>
    /// Builds an error response with the supplied status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    protected OperationResponseBase<TResponseData> CreateError(HttpStatusCode statusCode) => new() { StatusCode = statusCode};

    /// <summary>
    /// Builds an error response with the supplied status code and message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationResponseBase<TResponseData> CreateError(HttpStatusCode statusCode, string message) => new() { StatusCode = statusCode, ErrorMessages = [message] };

    /// <summary>
    /// Builds an error response that also carries a data payload (e.g. partial result or context).
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Data payload returned with the error.</param>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationResponseBase<TResponseData> CreateError(HttpStatusCode statusCode, TResponseData data, string message) => new() { StatusCode = statusCode, Data = data, ErrorMessages = [message] };

    /// <summary>
    /// Builds a 404 Not Found error response.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationResponseBase<TResponseData> CreateNotFound(string message) => CreateError(HttpStatusCode.NotFound, message);

    /// <summary>
    /// Builds a 401 Unauthorized error response.
    /// </summary>
    protected OperationResponseBase<TResponseData> CreateUnauthorized() => CreateError(HttpStatusCode.Unauthorized);

    /// <summary>
    /// Builds a 401 Unauthorized error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationResponseBase<TResponseData> CreateUnauthorized(string message) => CreateError(HttpStatusCode.Unauthorized, message);

    /// <summary>
    /// Builds a 401 Unauthorized error response carrying a data payload and message.
    /// </summary>
    /// <param name="data">Data payload returned with the error.</param>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationResponseBase<TResponseData> CreateUnauthorized(TResponseData data, string message) => CreateError(HttpStatusCode.Unauthorized, data, message);
}

/// <summary>
/// Base class for operations that return a list of items through <see cref="OperationListResponseBase{TResponseData}"/>.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponseData">Type of each item returned by the operation.</typeparam>
public abstract class OperationBaseWithDataList<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, OperationListResponseBase<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class
{
    /// <summary>
    /// Builds a successful list response with the supplied status code and items.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Items to include in the response payload.</param>
    protected OperationListResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData[]? data = null)
        => new() { StatusCode = statusCode, Data = data, Success = true };

    /// <summary>
    /// Builds a successful 200 OK list response with no items.
    /// </summary>
    protected OperationListResponseBase<TResponseData> CreateSuccessOk() => CreateSuccess(HttpStatusCode.OK);

    /// <summary>
    /// Materialises an async sequence into a successful 200 OK list response.
    /// </summary>
    /// <param name="dataAsAsyncEnumberable">Async sequence of items to include in the response.</param>
    protected async Task<OperationListResponseBase<TResponseData>> CreateSuccessOkAsync(IAsyncEnumerable<TResponseData> dataAsAsyncEnumberable)
    {
        List<TResponseData> data = [];
        await foreach (var item in dataAsAsyncEnumberable)
        {
            data.Add(item);
        }

        return CreateSuccess(HttpStatusCode.OK, [.. data]);
    }

    /// <summary>
    /// Builds a list error response with the supplied status code and message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationListResponseBase<TResponseData> CreateError(HttpStatusCode statusCode, string message)
        => new() { StatusCode = statusCode, ErrorMessages = [message] };

    /// <summary>
    /// Builds a 500 Internal Server Error list response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationListResponseBase<TResponseData> CreateInternalServerError(string message) => CreateError(HttpStatusCode.InternalServerError, message);
}

/// <summary>
/// Base class for operations that return a paginated list of items through <see cref="OperationPagedResponseBase{TResponseData}"/>.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponseData">Type of each item returned in the page.</typeparam>
public abstract class PagedOperationBase<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, OperationPagedResponseBase<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class
{
    /// <summary>
    /// Builds a successful paged response with the supplied status code and items.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Items to include in the current page.</param>
    protected OperationPagedResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData[]? data = null)
        => new() { StatusCode = statusCode, Data = data };

    /// <summary>
    /// Builds a successful 200 OK paged response with the supplied items.
    /// </summary>
    /// <param name="data">Items to include in the current page.</param>
    protected OperationPagedResponseBase<TResponseData> CreateSuccessOk(TResponseData[]? data = null) => CreateSuccess(HttpStatusCode.OK, data);

    /// <summary>
    /// Builds a paged error response with the supplied status code and message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationPagedResponseBase<TResponseData> CreateError(HttpStatusCode statusCode, string message)
        => new() { StatusCode = statusCode, ErrorMessages = [message] };

    /// <summary>
    /// Builds a 404 Not Found paged response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    protected OperationPagedResponseBase<TResponseData> CreateNotFound(string message) => CreateError(HttpStatusCode.NotFound, message);
}


/// <summary>
/// Represents the base for all operations that uses the default response (<see cref="OperationResponse"/>).
/// </summary>
public abstract class OperationBaseWithDefaultResponse<TRequest>(ILogger logger) : OperationBase<TRequest, OperationResponse>(logger) where TRequest : OperationRequestBase, new();

/// <summary>
/// Represents the base for all operations that does not use a request.
/// </summary>
/// <typeparam name="TResponse">Response type produced by the operation.</typeparam>
/// <param name="logger">Logger used to record failures raised while processing the operation.</param>
public abstract class OperationBaseWithoutRequest<TResponse>(ILogger logger) where TResponse : OperationResponse, new()
{
    /// <summary>
    /// Logger used to report failures raised while processing the operation.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// The main method of all operations.
    /// Asynchronously, orchestrates and validates the operations.
    /// </summary>
    /// <returns>The operation response.</returns>
    public async Task<TResponse> ProcessAsync()
    {
        try
        {
            return await ProcessOperationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
#if DEBUG
            string message = $"{ex}";
#else
            string message = "Ocorreu um erro interno durante o processamento da solicitação.";
#endif

            Logger.Error(message, ex);

            return OperationResponse.CreateInternalServerError<TResponse>(message);
        }
    }

    /// <summary>
    /// Asynchronously executes the use-case domain logic for this request-less operation.
    /// Implementations should use <see cref="Logger"/> for diagnostics and the
    /// <see cref="OperationResponse"/> factory helpers to build expected outcomes. Unexpected
    /// exceptions should propagate — they are caught by <see cref="ProcessAsync"/> and translated
    /// into a 500 Internal Server Error response.
    /// </summary>
    /// <returns>The operation response carrying the outcome of the domain logic.</returns>
    protected abstract Task<TResponse> ProcessOperationAsync();
}

/// <summary>
/// Represents the base for all operations that does not use a request.
/// </summary>
/// <param name="logger">Logger used to record failures raised while processing the operation.</param>
public abstract class DefaultOperationBase(ILogger logger)
{
    /// <summary>
    /// Logger used to report failures raised while processing the operation.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// The main method of all operations.
    /// Asynchronously, orchestrates and validates the operations.
    /// </summary>
    /// <returns>The operation response.</returns>
    public async Task<OperationResponse> ProcessAsync()
    {
        try
        {
            return await ProcessOperationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
#if DEBUG
            string message = $"{ex}";
#else
            string message = "Ocorreu um erro interno durante o processamento da solicitação.";
#endif

            Logger.Error(message, ex);

            return OperationResponse.CreateInternalServerError(message);
        }
    }

    /// <summary>
    /// Asynchronously executes the use-case domain logic for this default operation.
    /// Implementations should use <see cref="Logger"/> for diagnostics and the
    /// <see cref="OperationResponse"/> factory helpers to build expected outcomes. Unexpected
    /// exceptions should propagate — they are caught by <see cref="ProcessAsync"/> and translated
    /// into a 500 Internal Server Error response.
    /// </summary>
    /// <returns>The operation response carrying the outcome of the domain logic.</returns>
    protected abstract Task<OperationResponse> ProcessOperationAsync();
}
