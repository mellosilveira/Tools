using MelloSilveiraTools.Infrastructure.Logger;

namespace MelloSilveiraTools.UseCases.Operations;

/// <summary>
/// Represents the base for all operations in the application.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public abstract class OperationBase<TRequest, TResponse>(ILogger logger)
    where TRequest : OperationRequestBase, new()
    where TResponse : OperationResponse, new()
{
    /// <summary>
    /// The main method of all operations.
    /// Asynchronously, orchestrates and validates the operations.
    /// </summary>
    /// <param name="request">The operation request content.</param>
    /// <returns>The operation response.</returns>
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
            
            Dictionary<string, object> logAdditionalData = new() { { "Request", request } };
            logger.Error(message, ex, logAdditionalData);

            TResponse response = new();
            response.SetInternalServerError(message);
            return response;
        }
    }

    /// <summary>
    /// Asynchronously, processes the operation.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected abstract Task<TResponse> ProcessOperationAsync(TRequest request);

    /// <summary>
    /// Asynchronously, validates the request sent to operation.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected abstract Task<TResponse> ValidateOperationAsync(TRequest request);
}

/// <summary>
/// Represents the base for all operations that uses the default response (<see cref="OperationResponse"/>).
/// </summary>
public abstract class OperationBaseWithDefaultResponse<TRequest>(ILogger logger) : OperationBase<TRequest, OperationResponse>(logger) where TRequest : OperationRequestBase, new();

/// <summary>
/// Represents the base for all operations that does not use a request.
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public abstract class OperationBaseWithoutRequest<TResponse>(ILogger logger) where TResponse : OperationResponse, new()
{
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

            logger.Error(message, ex);

            TResponse response = new();
            response.SetInternalServerError(message);
            return response;
        }
    }

    /// <summary>
    /// Asynchronously, processes the operation.
    /// </summary>
    /// <returns></returns>
    protected abstract Task<TResponse> ProcessOperationAsync();
}
