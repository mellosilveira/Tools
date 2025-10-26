using MelloSilveiraTools.Infrastructure.Logger;
using System.Net;

namespace MelloSilveiraTools.Application.Operations;

/// <summary>
/// Represents the base for all operations in the application.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public abstract class OperationBase<TRequest, TResponse>(ILogger logger)
    where TRequest : OperationRequestBase, new()
    where TResponse : OperationResponse, new()
{
    protected ILogger Logger { get; } = logger;

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

            Dictionary<string, object?> logAdditionalData = new() { { "Request", request } };
            Logger.Error(message, ex, logAdditionalData);

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

public abstract class OperationBaseWithData<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, OperationResponseBase<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class
{
    protected OperationResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData? data = null)
        => new() { StatusCode = statusCode, Data = data };

    protected OperationResponseBase<TResponseData> CreateSuccessOk(TResponseData? data = null) => CreateSuccess(HttpStatusCode.OK, data);

    protected OperationResponseBase<TResponseData> CreateError(HttpStatusCode statusCode) => new() { StatusCode = statusCode};

    protected OperationResponseBase<TResponseData> CreateError(HttpStatusCode statusCode, string message) => new() { StatusCode = statusCode, ErrorMessages = [message] };

    protected OperationResponseBase<TResponseData> CreateNotFound(string message) => CreateError(HttpStatusCode.NotFound, message);

    protected OperationResponseBase<TResponseData> CreateUnauthorized() => CreateError(HttpStatusCode.Unauthorized);
    
    protected OperationResponseBase<TResponseData> CreateUnauthorized(string message) => CreateError(HttpStatusCode.Unauthorized, message);
}

public abstract class OperationBaseWithDataList<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, OperationListResponseBase<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class
{
    protected OperationListResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData[]? data = null)
        => new() { StatusCode = statusCode, Data = data };

    protected OperationListResponseBase<TResponseData> CreateSuccessOk() => CreateSuccess(HttpStatusCode.OK);

    protected async Task<OperationListResponseBase<TResponseData>> CreateSuccessOkAsync(IAsyncEnumerable<TResponseData> dataAsAsyncEnumberable)
    {
        List<TResponseData> data = [];
        await foreach (var item in dataAsAsyncEnumberable)
        {
            data.Add(item); 
        }

        return CreateSuccess(HttpStatusCode.OK, [.. data]);
    }

    protected OperationListResponseBase<TResponseData> CreateError(HttpStatusCode statusCode, string message)
        => new() { StatusCode = statusCode, ErrorMessages = [message] };

    protected OperationListResponseBase<TResponseData> CreateInternalServerError(string message) => CreateError(HttpStatusCode.InternalServerError, message);
}

public abstract class PagedOperationBase<TRequest, TResponseData>(ILogger logger) : OperationBase<TRequest, OperationPagedResponseBase<TResponseData>>(logger)
    where TRequest : OperationRequestBase, new()
    where TResponseData : class
{
    protected OperationPagedResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData[]? data = null)
        => new() { StatusCode = statusCode, Data = data };

    protected OperationPagedResponseBase<TResponseData> CreateSuccessOk(TResponseData[]? data = null) => CreateSuccess(HttpStatusCode.OK, data);

    protected OperationPagedResponseBase<TResponseData> CreateError(HttpStatusCode statusCode, string message)
        => new() { StatusCode = statusCode, ErrorMessages = [message] };

    protected OperationPagedResponseBase<TResponseData> CreateNotFound(string message) => CreateError(HttpStatusCode.NotFound, message);
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
