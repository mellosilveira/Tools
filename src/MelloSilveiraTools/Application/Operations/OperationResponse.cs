using MelloSilveiraTools.ExtensionMethods;
using System.Net;

namespace MelloSilveiraTools.Application.Operations;

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
    public bool Success { get; init; }

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// The list of error message.
    /// </summary>
    public List<string> ErrorMessages { get; init; }

    public static OperationResponse CreateSuccess(HttpStatusCode statusCode) => new() { StatusCode = statusCode, Success = true };

    public static OperationResponse CreateError(HttpStatusCode statusCode) => new() { StatusCode = statusCode, Success = false };

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

    public static OperationResponse CreateSuccessOk() => CreateSuccess(HttpStatusCode.OK);

    public static OperationResponse CreateSuccessCreated() => CreateSuccess(HttpStatusCode.Created);

    public static OperationResponse CreateNoContent() => CreateSuccess(HttpStatusCode.NoContent);

    public static OperationResponse CreateUnauthorized() => CreateError(HttpStatusCode.Unauthorized);

    public static OperationResponse CreateUnauthorized(string message) => CreateError(HttpStatusCode.Unauthorized, message);

    public static OperationResponse CreateNotFound(string message) => CreateError(HttpStatusCode.NotFound, message);

    public static OperationResponse CreateRequestTimeout(string message) => CreateError(HttpStatusCode.RequestTimeout, message);

    public static OperationResponse CreateUnprocessableEntity(string message) => CreateError(HttpStatusCode.UnprocessableEntity, message);

    public static OperationResponse CreateUnprocessableEntity(List<string> messages) => CreateError(HttpStatusCode.UnprocessableEntity, messages);

    public static OperationResponse CreateInternalServerError(string message) => CreateError(HttpStatusCode.InternalServerError, message);

    public static OperationResponse CreateServiceUnavailable(string message) => CreateError(HttpStatusCode.ServiceUnavailable, message);

    public static OperationListResponseBase<TResponseData> CreateListSuccessOk<TResponseData>(TResponseData[]? data = null)
        where TResponseData : class
        => new() { Data = data, StatusCode = HttpStatusCode.OK, Success = true };

    public static TResponse CreateListSuccess<TResponse, TResponseData>(HttpStatusCode statusCode, TResponseData[]? data = null)
        where TResponse : OperationListResponseBase<TResponseData>, new()
        where TResponseData : class
        => new() { Data = data, StatusCode = statusCode, Success = true };

    public static TResponse CreateListSuccessOk<TResponse, TResponseData>(TResponseData[]? data = null)
        where TResponse : OperationListResponseBase<TResponseData>, new()
        where TResponseData : class
        => CreateListSuccess<TResponse, TResponseData>(HttpStatusCode.OK, data);

    public static TResponse CreateSuccessOk<TResponse>() where TResponse : OperationResponse, new() => new() { StatusCode = HttpStatusCode.OK, Success = true };

    public static OperationResponseBase<TResponseData> CreateSuccessOk<TResponseData>(TResponseData responseData) where TResponseData : class
        => new() { StatusCode = HttpStatusCode.OK, Data = responseData, Success = true };

    public static TResponse CreateError<TResponse>(HttpStatusCode statusCode, string message) where TResponse : OperationResponse, new() => new()
    {
        StatusCode = statusCode,
        ErrorMessages = [message],
        Success = false
    };

    public static TResponse CreateNotFound<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.NotFound, message);

    public static TResponse CreateRequestTimeout<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.RequestTimeout, message);

    public static TResponse CreateConflict<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.Conflict, message);

    public static TResponse CreateUnprocessableEntity<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.UnprocessableEntity, message);

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

    public static OperationResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData? data = null)
        => new() { Data = data, StatusCode = statusCode };
}

public record OperationListResponseBase<TResponseData> : OperationResponseBase<TResponseData[]> where TResponseData : class
{
    public long Count => Data?.LongLength ?? 0;
}

public record OperationPagedResponseBase<TResponseData> : OperationListResponseBase<TResponseData> where TResponseData : class
{
    public long TotalCount { get; init; }

    public long PageNumber { get; init; }

    public long PageSize { get; init; }
}
