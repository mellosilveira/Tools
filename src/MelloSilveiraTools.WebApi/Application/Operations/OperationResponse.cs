using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Operations;

/// <summary>
/// Response content for all operations.
/// </summary>
public record OperationResponseBase
{
    /// <summary>
    /// The success status of operation.
    /// </summary>
    public bool Success { get; init; } = false;

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    public List<string> Messages { get; init; } = [];
}

public record OperationResponse : OperationResponseBase
{
    /// <summary>
    /// Creates a successful 200 OK response.
    /// </summary>
    public static TResponse CreateSuccessOk<TResponse>() where TResponse : OperationResponseBase, new()
        => new() { StatusCode = HttpStatusCode.OK, Success = true };

    /// <summary>
    /// Creates a 500 Internal Server Error response with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static TResponse CreateInternalServerError<TResponse>(string message) where TResponse : OperationResponseBase, new()
        => new() { StatusCode = HttpStatusCode.InternalServerError, Messages = [message], Success = false };

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
    /// Creates an error response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="message">Error messages describing the failures.</param>
    public static OperationResponse CreateError(HttpStatusCode statusCode, string? message) => new()
    {
        StatusCode = statusCode,
        Messages = message is null ? [] : [message]
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
    public static OperationResponse CreateNotFound(string? message = null) => CreateError(HttpStatusCode.NotFound, message);

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
    /// Creates a successful response carrying the supplied data payload.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="responseData">Data returned to the caller.</param>
    public static OperationResponse<TResponseData> CreateSuccess<TResponseData>(HttpStatusCode statusCode, TResponseData? responseData = null) where TResponseData : class
        => new() { StatusCode = statusCode, Data = responseData, Success = true };

    /// <summary>
    /// Creates a successful 200 OK response carrying the supplied data payload.
    /// </summary>
    /// <param name="responseData">Data returned to the caller.</param>
    public static OperationResponse<TResponseData> CreateSuccessOk<TResponseData>(TResponseData? responseData = null) where TResponseData : class
        => CreateSuccess(HttpStatusCode.OK, responseData);

    /// <summary>
    /// Creates a successful 201 Created response.
    /// </summary>
    public static OperationResponse<TResponseData> CreateSuccessCreated<TResponseData>(TResponseData responseData) where TResponseData : class 
        => CreateSuccess(HttpStatusCode.Created, responseData);

    /// <summary>
    /// Creates a typed 409 Conflict error response.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message">Error message describing the failure.</param>
    public static OperationResponse<TResponseData> CreateConflict<TResponseData>(TResponseData data, string message) where TResponseData : class, new() 
        => new() { Data = data, Messages = [message], StatusCode = HttpStatusCode.Conflict, Success = false };

    /// <summary>
    /// Creates a successful list response of a specific type with the supplied items.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Items to include in the response payload.</param>
    public static ListedOperationResponse<TResponseData> CreateListedSuccess<TResponseData>(HttpStatusCode statusCode, IEnumerable<TResponseData>? data = null) where TResponseData : class
        => new() { Data = data?.ToList(), StatusCode = statusCode, Success = true };

    /// <summary>
    /// Creates a successful 200 OK list response containing the supplied items.
    /// </summary>
    /// <param name="data">Items to include in the response payload.</param>
    public static ListedOperationResponse<TResponseData> CreateListedSuccessOk<TResponseData>(IEnumerable<TResponseData>? data = null) where TResponseData : class
        => CreateListedSuccess(HttpStatusCode.OK, data);

    /// <summary>
    /// Builds a successful 200 OK paged response with the supplied items.
    /// </summary>
    /// <param name="data">Items to include in the current page.</param>
    public static PagedOperationResponse<TResponseData> CreatePagedSuccessOk<TResponseData>(IEnumerable<TResponseData>? data = null) where TResponseData : class, new()
        => new() { StatusCode = HttpStatusCode.OK, Data = data?.ToList() };
}

/// <summary>
/// Response content for all operations.
/// </summary>
/// <typeparam name="TResponseData"></typeparam>
public record OperationResponse<TResponseData> : OperationResponseBase where TResponseData : class
{
    /// <summary>
    /// Data content of all operation response.
    /// </summary>
    public TResponseData? Data { get; init; }

    public static implicit operator OperationResponse<TResponseData>(OperationResponse response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}

/// <summary>
/// Base response for operations that return an array of items.
/// </summary>
/// <typeparam name="TResponseData">Type of each item in the returned list.</typeparam>
public sealed record ListedOperationResponse<TResponseData> : OperationResponseBase where TResponseData : class
{
    /// <summary>
    /// Data content of all operation response.
    /// </summary>
    public List<TResponseData>? Data { get; init; }

    /// <summary>
    /// Number of items returned in <see cref="OperationResponse{T}.Data"/>.
    /// </summary>
    public long Count => Data?.Count ?? 0;

    public static implicit operator ListedOperationResponse<TResponseData>(OperationResponse response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}

/// <summary>
/// Base response for operations that return a paginated list of items.
/// </summary>
/// <typeparam name="TResponseData">Type of each item in the returned page.</typeparam>
public sealed record PagedOperationResponse<TResponseData> : OperationResponseBase where TResponseData : class
{
    /// <summary>
    /// Data content of all operation response.
    /// </summary>
    public List<TResponseData>? Data { get; init; }

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
    public long PageSize => Data?.Count ?? 0;

    public static implicit operator PagedOperationResponse<TResponseData>(OperationResponse response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}
