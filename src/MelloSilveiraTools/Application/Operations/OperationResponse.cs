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
    /// The list of error messages.
    /// </summary>
    public List<string> ErrorMessages { get; init; }

    /// <summary>
    /// Creates a successful response with the supplied status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    public static OperationResponse CreateSuccess(HttpStatusCode statusCode) => new() { StatusCode = statusCode, Success = true };

    /// <summary>
    /// Creates an error response with the supplied status code and no error messages.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    public static OperationResponse CreateError(HttpStatusCode statusCode) => new() { StatusCode = statusCode, Success = false };

    /// <summary>
    /// Creates an error response with a single error message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse CreateError(HttpStatusCode statusCode, string message) => new()
    {
        StatusCode = statusCode,
        ErrorMessages = [message]
    };

    /// <summary>
    /// Creates an error response with a collection of error messages.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="messages">Error messages describing the failures.</param>
    public static OperationResponse CreateError(HttpStatusCode statusCode, List<string> messages) => new()
    {
        StatusCode = statusCode,
        ErrorMessages = messages
    };

    /// <summary>
    /// Creates a successful 200 OK response.
    /// </summary>
    public static OperationResponse CreateSuccessOk() => CreateSuccess(HttpStatusCode.OK);

    /// <summary>
    /// Creates a successful 201 Created response.
    /// </summary>
    public static OperationResponse CreateSuccessCreated() => CreateSuccess(HttpStatusCode.Created);

    /// <summary>
    /// Creates a successful 204 No Content response.
    /// </summary>
    public static OperationResponse CreateNoContent() => CreateSuccess(HttpStatusCode.NoContent);

    /// <summary>
    /// Creates a 401 Unauthorized error response without a message.
    /// </summary>
    public static OperationResponse CreateUnauthorized() => CreateError(HttpStatusCode.Unauthorized);

    /// <summary>
    /// Creates a 401 Unauthorized error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse CreateUnauthorized(string message) => CreateError(HttpStatusCode.Unauthorized, message);

    /// <summary>
    /// Creates a 404 Not Found error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse CreateNotFound(string message) => CreateError(HttpStatusCode.NotFound, message);

    /// <summary>
    /// Creates a 408 Request Timeout error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse CreateRequestTimeout(string message) => CreateError(HttpStatusCode.RequestTimeout, message);

    /// <summary>
    /// Creates a 422 Unprocessable Entity error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse CreateUnprocessableEntity(string message) => CreateError(HttpStatusCode.UnprocessableEntity, message);

    /// <summary>
    /// Creates a 422 Unprocessable Entity error response with the supplied messages.
    /// </summary>
    /// <param name="messages">Error messages describing the failures.</param>
    public static OperationResponse CreateUnprocessableEntity(List<string> messages) => CreateError(HttpStatusCode.UnprocessableEntity, messages);

    /// <summary>
    /// Creates a 500 Internal Server Error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse CreateInternalServerError(string message) => CreateError(HttpStatusCode.InternalServerError, message);

    /// <summary>
    /// Creates a 503 Service Unavailable error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse CreateServiceUnavailable(string message) => CreateError(HttpStatusCode.ServiceUnavailable, message);

    /// <summary>
    /// Creates a successful 200 OK list response containing the supplied items.
    /// </summary>
    /// <param name="data">Items to include in the response payload.</param>
    public static OperationListResponseBase<TResponseData> CreateListSuccessOk<TResponseData>(TResponseData[]? data = null)
        where TResponseData : class
        => new() { Data = data, StatusCode = HttpStatusCode.OK, Success = true };

    /// <summary>
    /// Creates a successful list response of a specific type with the supplied items.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Items to include in the response payload.</param>
    public static TResponse CreateListSuccess<TResponse, TResponseData>(HttpStatusCode statusCode, TResponseData[]? data = null)
        where TResponse : OperationListResponseBase<TResponseData>, new()
        where TResponseData : class
        => new() { Data = data, StatusCode = statusCode, Success = true };

    /// <summary>
    /// Creates a successful 200 OK list response of a specific type with the supplied items.
    /// </summary>
    /// <param name="data">Items to include in the response payload.</param>
    public static TResponse CreateListSuccessOk<TResponse, TResponseData>(TResponseData[]? data = null)
        where TResponse : OperationListResponseBase<TResponseData>, new()
        where TResponseData : class
        => CreateListSuccess<TResponse, TResponseData>(HttpStatusCode.OK, data);

    /// <summary>
    /// Creates a successful 200 OK response of a specific response type.
    /// </summary>
    public static TResponse CreateSuccessOk<TResponse>() where TResponse : OperationResponse, new() => new() { StatusCode = HttpStatusCode.OK, Success = true };

    /// <summary>
    /// Creates a successful 200 OK response carrying the supplied data payload.
    /// </summary>
    /// <param name="responseData">Data returned to the caller.</param>
    public static OperationResponseBase<TResponseData> CreateSuccessOk<TResponseData>(TResponseData responseData) where TResponseData : class
        => new() { StatusCode = HttpStatusCode.OK, Data = responseData, Success = true };

    /// <summary>
    /// Creates a typed error response with the supplied status code and message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="message">Error message describing the failure.</param>
    public static TResponse CreateError<TResponse>(HttpStatusCode statusCode, string message) where TResponse : OperationResponse, new() => new()
    {
        StatusCode = statusCode,
        ErrorMessages = [message],
        Success = false
    };

    /// <summary>
    /// Creates a typed 404 Not Found error response.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static TResponse CreateNotFound<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.NotFound, message);

    /// <summary>
    /// Creates a typed 408 Request Timeout error response.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static TResponse CreateRequestTimeout<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.RequestTimeout, message);

    /// <summary>
    /// Creates a typed 409 Conflict error response.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static TResponse CreateConflict<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.Conflict, message);

    /// <summary>
    /// Creates a typed 422 Unprocessable Entity error response.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static TResponse CreateUnprocessableEntity<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.UnprocessableEntity, message);

    /// <summary>
    /// Creates a typed 500 Internal Server Error response.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static TResponse CreateInternalServerError<TResponse>(string message) where TResponse : OperationResponse, new() => CreateError<TResponse>(HttpStatusCode.InternalServerError, message);

    /// <summary>
    /// Creates a typed 503 Service Unavailable error response.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
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

    /// <summary>
    /// Creates a successful response carrying the supplied data payload.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Data returned to the caller.</param>
    public static OperationResponseBase<TResponseData> CreateSuccess(HttpStatusCode statusCode, TResponseData? data = null)
        => new() { Data = data, StatusCode = statusCode };
}

/// <summary>
/// Base response for operations that return an array of items.
/// </summary>
/// <typeparam name="TResponseData">Type of each item in the returned list.</typeparam>
public record OperationListResponseBase<TResponseData> : OperationResponseBase<TResponseData[]> where TResponseData : class
{
    /// <summary>
    /// Number of items returned in <see cref="OperationResponseBase{T}.Data"/>.
    /// </summary>
    public long Count => Data?.LongLength ?? 0;
}

/// <summary>
/// Base response for operations that return a paginated list of items.
/// </summary>
/// <typeparam name="TResponseData">Type of each item in the returned page.</typeparam>
public record OperationPagedResponseBase<TResponseData> : OperationListResponseBase<TResponseData> where TResponseData : class
{
    /// <summary>
    /// Total number of items that match the query across all pages.
    /// </summary>
    public long TotalCount { get; init; }

    /// <summary>
    /// One-based number of the current page.
    /// </summary>
    public long PageNumber { get; init; }

    /// <summary>
    /// Number of items in the current page.
    /// </summary>
    public long PageSize { get; init; }
}
