using MelloSilveiraTools.ExtensionMethods;
using System.Net;

namespace MelloSilveiraTools.UseCases.Operations;

/// <summary>
/// Response content for all operations.
/// </summary>
public record OperationResponse
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResponse"/>.
    /// </summary>
    public OperationResponse()
    {
        ErrorMessages = [];
    }

    /// <summary>
    /// The success status of operation.
    /// </summary>
    public bool Success => ErrorMessages.IsEmpty();

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; protected set; }

    /// <summary>
    /// The list of error message.
    /// </summary>
    public List<string> ErrorMessages { get; init; }

    public void SetStatusCode(HttpStatusCode statusCode) => StatusCode = statusCode;

    public void SetInternalServerError(string message)
    {
        StatusCode = HttpStatusCode.InternalServerError;
        ErrorMessages.Add(message);
    }

    public static OperationResponse CreateSuccess(HttpStatusCode statusCode) => new() { StatusCode = statusCode };

    public static OperationResponse CreateError(HttpStatusCode statusCode, string message) => new()
    {
        StatusCode = statusCode,
        ErrorMessages = [message]
    };

    public static OperationResponse CreateError(HttpStatusCode statusCode, List<string> messages) => new()
    {
        StatusCode = statusCode,
        ErrorMessages = messages
    };

    public static OperationResponse CreateSuccessOk() => new() { StatusCode = HttpStatusCode.OK };

    public static OperationResponse CreateNotFound(string message) => CreateError(HttpStatusCode.NotFound, message);

    public static OperationResponse CreateRequestTimeout(string message) => CreateError(HttpStatusCode.RequestTimeout, message);

    public static OperationResponse CreateUnprocessableEntity(string message) => CreateError(HttpStatusCode.UnprocessableEntity, message);

    public static OperationResponse CreateUnprocessableEntity(List<string> messages) => CreateError(HttpStatusCode.UnprocessableEntity, messages);

    public static OperationResponse CreateInternalServerError(string message) => CreateError(HttpStatusCode.InternalServerError, message);

    public static OperationResponse CreateServiceUnavailable(string message) => CreateError(HttpStatusCode.ServiceUnavailable, message);

    public static TResponse CreateSuccess<TResponse, TResponseData>(HttpStatusCode statusCode, TResponseData? data = null)
        where TResponse : OperationResponseBase<TResponseData>, new()
        where TResponseData : class
    {
        return new()
        {
            Data = data,
            StatusCode = statusCode,
        };
    }

    public static TResponse CreateListSuccess<TResponse, TResponseData>(HttpStatusCode statusCode, TResponseData[]? data = null)
        where TResponse : OperationListResponseBase<TResponseData>, new()
        where TResponseData : class
    {
        return new()
        {
            Data = data,
            StatusCode = statusCode,
        };
    }

    public static TResponse CreateSuccessOk<TResponse>() where TResponse : OperationResponse, new() => new() { StatusCode = HttpStatusCode.OK };

    public static TResponse CreateSuccessOk<TResponse, TResponseData>(TResponseData? data = null)
        where TResponse : OperationResponseBase<TResponseData>, new()
        where TResponseData : class
    {
        return CreateSuccess<TResponse, TResponseData>(HttpStatusCode.OK, data);
    }

    public static TResponse CreateError<TResponse>(HttpStatusCode statusCode, string message) where TResponse : OperationResponse, new() => new()
    {
        StatusCode = statusCode,
        ErrorMessages = [message]
    };

    public static TResponse CreateNotFound<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.NotFound, message);

    public static TResponse CreateRequestTimeout<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.RequestTimeout, message);

    public static TResponse CreateInternalServerError<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.InternalServerError, message);

    public static TResponse CreateServiceUnavailable<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.ServiceUnavailable, message);
}

/// <summary>
/// Response content for all operations.
/// </summary>
/// <typeparam name="TResponseData"></typeparam>
public record OperationResponseBase<TResponseData> : OperationResponse where TResponseData : class
{
    /// <summary>
    /// Data content of all operation response.
    /// </summary>
    public TResponseData? Data { get; init; }
}

public record OperationListResponseBase<TResponseData> : OperationResponseBase<TResponseData[]> where TResponseData : class
{
    public long Count => Data?.LongLength ?? 0;
}
