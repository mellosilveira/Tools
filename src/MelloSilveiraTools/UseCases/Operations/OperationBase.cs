using System.Globalization;

namespace MelloSilveiraTools.UseCases.Operations;

/// <summary>
/// Represents the base for all operations in the application.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public abstract class OperationBase<TRequest, TResponse>
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
            string message = "An internal server occurred while processing the request.";
#endif
            return OperationResponse.CreateWithInternalServerError<TResponse>(message);
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
public abstract class OperationBaseWithDefaultResponse<TRequest> : OperationBase<TRequest, OperationResponse> where TRequest : OperationRequestBase, new();

/// <summary>
/// Represents the base for all operations that does not use a request.
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public abstract class OperationBaseWithoutRequest<TResponse> where TResponse : OperationResponse, new()
{
    /// <summary>
    /// The main method of all operations.
    /// Asynchronously, orchestrates and validates the operations.
    /// </summary>
    /// <returns>The operation response.</returns>
    public async Task<TResponse> ProcessAsync()
    {
        // Sets the current culture like invariant.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        TResponse response;

        try
        {
            response = await ProcessOperationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
#if DEBUG
            string message = $"{ex}";
#else
            string message = "An internal server occurred while processing the request.";
#endif

            return OperationResponse.CreateWithInternalServerError<TResponse>(message);
        }

        return response;
    }

    /// <summary>
    /// Asynchronously, processes the operation.
    /// </summary>
    /// <returns></returns>
    protected abstract Task<TResponse> ProcessOperationAsync();
}
