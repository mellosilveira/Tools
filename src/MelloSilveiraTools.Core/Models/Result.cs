namespace MelloSilveiraTools.Core.Models;

// TODO: ANALISAR VIABILIDADE DISSO
//public class AsyncResult : Task<Result>
//{
//    /// <summary>
//    /// Creates a successful 200 OK result.
//    /// </summary>
//    public static AsyncResult CreateSuccessOk() => Task.FromResult(Result.CreateSuccessOk());
//}

/// <summary>
/// Result content for all commands.
/// </summary>
public abstract record ResultBase
{
    public bool Success { get; init; } = false;

    public StatusCode StatusCode { get; init; }

    public List<string> Messages { get; init; } = [];
}

public record Result : ResultBase
{
    /// <summary>
    /// Creates a successful 200 OK result.
    /// </summary>
    public static TResult CreateSuccessOk<TResult>() where TResult : ResultBase, new()
        => new() { StatusCode = StatusCode.OK, Success = true };

    /// <summary>
    /// Creates a 400 Bad Request result with the supplied message.
    /// </summary>
    /// <param name="messages">Error messages describing the failure.</param>
    public static TResult CreateBadRequest<TResult>(List<string> messages) where TResult : ResultBase, new()
        => new() { StatusCode = StatusCode.BadRequest, Messages = messages, Success = false };

    /// <summary>
    /// Creates a 500 Internal Server Error result with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static TResult CreateUnknownError<TResult>(string message) where TResult : ResultBase, new()
        => new() { StatusCode = StatusCode.UnknownError, Messages = [message], Success = false };

    /// <summary>
    /// Creates a successful result with the supplied status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    public static Result CreateSuccess(StatusCode statusCode) => new() { StatusCode = statusCode, Success = true };

    /// <summary>
    /// Creates an error result with the supplied status code and no error messages.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    public static Result CreateError(StatusCode statusCode) => new() { StatusCode = statusCode, Success = false };

    /// <summary>
    /// Creates an error result.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="message">Error messages describing the failures.</param>
    public static Result CreateError(StatusCode statusCode, string? message) => new()
    {
        StatusCode = statusCode,
        Messages = message is null ? [] : [message]
    };

    /// <summary>
    /// Creates an error result.
    /// </summary>0
    public static TResult Create<TResult>(Result result) where TResult : ResultBase, new()
        => new() { Success = result.Success, StatusCode = result.StatusCode, Messages = result.Messages ?? [] };

    /// <summary>
    /// Creates a successful 200 OK result.
    /// </summary>
    public static Result CreateSuccessOk() => CreateSuccess(StatusCode.OK);

    /// <summary>
    /// Creates a successful 201 Created result.
    /// </summary>
    public static Result CreateSuccessCreated() => CreateSuccess(StatusCode.Created);

    /// <summary>
    /// Creates a successful 204 No Content result.
    /// </summary>
    public static Result CreateNoContent() => CreateSuccess(StatusCode.NoContent);

    /// <summary>
    /// Creates a 400 BadRequest error result.
    /// </summary>
    public static Result CreateBadRequest(string message) => CreateError(StatusCode.Unauthorized, message);

    /// <summary>
    /// Creates a 401 Unauthorized error result without a message.
    /// </summary>
    public static Result CreateUnauthorized() => CreateError(StatusCode.Unauthorized);

    /// <summary>
    /// Creates a 401 Unauthorized error result with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static Result CreateUnauthorized(string message) => CreateError(StatusCode.Unauthorized, message);

    /// <summary>
    /// Creates a 404 Not Found error result with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static Result CreateNotFound(string? message = null) => CreateError(StatusCode.NotFound, message);

    /// <summary>
    /// Creates a 408 Request Timeout error result with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static Result CreateRequestTimeout(string message) => CreateError(StatusCode.RequestTimeout, message);

    /// <summary>
    /// Creates a 422 Unprocessable Entity error result with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static Result CreateUnprocessableEntity(string message) => CreateError(StatusCode.UnprocessableEntity, message);

    /// <summary>
    /// Creates a 500 Internal Server Error result with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static Result CreateUnknownError(string message) => CreateError(StatusCode.UnknownError, message);

    /// <summary>
    /// Creates a 503 Service Unavailable error result with the supplied message.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public static Result CreateServiceUnavailable(string message) => CreateError(StatusCode.ServiceUnavailable, message);

    /// <summary>
    /// Creates a successful result carrying the supplied data payload.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="resultData">Data returned to the caller.</param>
    public static Result<TResultData> CreateSuccess<TResultData>(StatusCode statusCode, TResultData? resultData = null) where TResultData : class
        => new() { StatusCode = statusCode, Data = resultData, Success = true };

    /// <summary>
    /// Creates a successful 200 OK result carrying the supplied data payload.
    /// </summary>
    /// <param name="resultData">Data returned to the caller.</param>
    public static Result<TResultData> CreateSuccessOk<TResultData>(TResultData? resultData = null) where TResultData : class
        => CreateSuccess(StatusCode.OK, resultData);

    /// <summary>
    /// Creates a successful 201 Created result.
    /// </summary>
    public static Result<TResultData> CreateSuccessCreated<TResultData>(TResultData resultData) where TResultData : class
        => CreateSuccess(StatusCode.Created, resultData);

    /// <summary>
    /// Creates a typed 409 Conflict error result.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message">Error message describing the failure.</param>
    public static Result<TResultData> CreateConflict<TResultData>(TResultData data, string message) where TResultData : class, new()
        => new() { Data = data, Messages = [message], StatusCode = StatusCode.Conflict, Success = false };

    /// <summary>
    /// Creates a successful list result of a specific type with the supplied items.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned to the caller.</param>
    /// <param name="data">Items to include in the result payload.</param>
    public static ListedResult<TResultData> CreateListedSuccess<TResultData>(StatusCode statusCode, IEnumerable<TResultData>? data = null) where TResultData : class
        => new() { Data = data?.ToList(), StatusCode = statusCode, Success = true };

    /// <summary>
    /// Creates a successful 200 OK list result containing the supplied items.
    /// </summary>
    /// <param name="data">Items to include in the result payload.</param>
    public static ListedResult<TResultData> CreateListedSuccessOk<TResultData>(IEnumerable<TResultData>? data = null) where TResultData : class
        => CreateListedSuccess(StatusCode.OK, data);

    /// <summary>
    /// Builds a successful 200 OK paged result with the supplied items.
    /// </summary>
    /// <param name="data">Items to include in the current page.</param>
    public static PagedResult<TResultData> CreatePagedSuccessOk<TResultData>(IEnumerable<TResultData>? data = null) where TResultData : class, new()
        => new() { StatusCode = StatusCode.OK, Data = data?.ToList() };
}

/// <summary>
/// Result content for all commands.
/// </summary>
/// <typeparam name="TResultData"></typeparam>
public record Result<TResultData> : ResultBase where TResultData : class
{
    /// <summary>
    /// Data content of all result.
    /// </summary>
    public TResultData? Data { get; init; }

    public static implicit operator Result<TResultData>(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}

/// <summary>
/// Base result for commands that return an array of items.
/// </summary>
/// <typeparam name="TResultData">Type of each item in the returned list.</typeparam>
public record ListedResult<TResultData> : ResultBase where TResultData : class
{
    /// <summary>
    /// Data content of all result.
    /// </summary>
    public List<TResultData>? Data { get; init; }

    /// <summary>
    /// Number of items returned in <see cref="Result{T}.Data"/>.
    /// </summary>
    public long Count => Data?.Count ?? 0;

    public static implicit operator ListedResult<TResultData>(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}

/// <summary>
/// Base result for commands that return a paginated list of items.
/// </summary>
/// <typeparam name="TResultData">Type of each item in the returned page.</typeparam>
public record PagedResult<TResultData> : ListedResult<TResultData> where TResultData : class
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

    public static implicit operator PagedResult<TResultData>(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}
