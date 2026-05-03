using MelloSilveiraTools.Core.Logger;
using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Operations;

/// <summary>
/// Represents the base for all operations in the application.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponse">Response type produced by the operation.</typeparam>
/// <param name="logger">Logger used to record failures raised while processing the operation.</param>
public abstract class OperationBase<TRequest, TResponse>(ILogger logger)
    where TRequest : OperationRequestBase, new()
    where TResponse : OperationResponseBase, new()
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
/// Base class for operations that return an <see cref="OperationResponse{TResponseData}"/> carrying a single data payload.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponseData">Type of the data payload returned by the operation.</typeparam>
public abstract class OperationBaseWithData<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, OperationResponse<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class, new()
{ }

/// <summary>
/// Base class for operations that return a list of items through <see cref="ListedOperationResponse{TResponseData}"/>.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponseData">Type of each item returned by the operation.</typeparam>
public abstract class ListedOperationBase<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, ListedOperationResponse<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class, new()
{ }

/// <summary>
/// Base class for operations that return a paginated list of items through <see cref="PagedOperationResponse{TResponseData}"/>.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the operation.</typeparam>
/// <typeparam name="TResponseData">Type of each item returned in the page.</typeparam>
public abstract class PagedOperationBase<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, PagedOperationResponse<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class, new()
{ }

/// <summary>
/// Represents the base for all operations that uses the default response (<see cref="OperationResponse"/>).
/// </summary>
public abstract class OperationBaseWithDefaultResponse<TRequest>(ILogger logger) : OperationBase<TRequest, OperationResponse>(logger) where TRequest : OperationRequestBase, new();

/// <summary>
/// Represents the base for all operations that does not use a request.
/// </summary>
/// <param name="logger">Logger used to record failures raised while processing the operation.</param>
public abstract class OperationBaseWithoutRequest<TResponseData>(ILogger logger) where TResponseData : class, new()
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
    public async Task<OperationResponse<TResponseData>> ProcessAsync()
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
    /// Asynchronously executes the use-case domain logic for this request-less operation.
    /// Implementations should use <see cref="Logger"/> for diagnostics and the
    /// <see cref="OperationResponse"/> factory helpers to build expected outcomes. Unexpected
    /// exceptions should propagate — they are caught by <see cref="ProcessAsync"/> and translated
    /// into a 500 Internal Server Error response.
    /// </summary>
    /// <returns>The operation response carrying the outcome of the domain logic.</returns>
    protected abstract Task<OperationResponse<TResponseData>> ProcessOperationAsync();
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
    /// Asynchronously executes the use-case domain logic for this request-less operation.
    /// Implementations should use <see cref="Logger"/> for diagnostics and the
    /// <see cref="OperationResponse"/> factory helpers to build expected outcomes. Unexpected
    /// exceptions should propagate — they are caught by <see cref="ProcessAsync"/> and translated
    /// into a 500 Internal Server Error response.
    /// </summary>
    /// <returns>The operation response carrying the outcome of the domain logic.</returns>
    protected abstract Task<OperationResponse> ProcessOperationAsync();
}
