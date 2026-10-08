namespace MelloSilveiraTools.Core.Models;

/// <summary>
/// Represents the standardized business outcome of an operation or process.
/// Provides clear visibility into whether an action succeeded or encountered business constraints,
/// accompanied by actionable feedback messages for end users and client systems.
/// </summary>
public abstract record ResultBase
{
    /// <summary>
    /// Indicates whether the business objective of the operation was completed successfully.
    /// </summary>
    public bool Success { get; init; } = false;

    /// <summary>
    /// Status code describing the business condition (e.g. Success, Validation Issue, Item Not Found).
    /// </summary>
    public StatusCode StatusCode { get; init; }

    /// <summary>
    /// Clear and actionable messages explaining the outcome or detailing what needs attention.
    /// </summary>
    public List<string> Messages { get; init; } = [];

    /// <summary>
    /// Indicates whether the action was halted due to conflicting business data (such as a duplicate record).
    /// </summary>
    public bool IsConflict => StatusCode == StatusCode.Conflict;
}

/// <summary>
/// Central utility for creating consistent and understandable business operation results.
/// </summary>
public record Result : ResultBase
{
    /// <summary>
    /// Creates a successful outcome confirming that the requested business action completed normally.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <returns>A successful business result.</returns>
    public static TResult CreateSuccessOk<TResult>() where TResult : ResultBase, new()
        => new() { StatusCode = StatusCode.OK, Success = true };

    /// <summary>
    /// Creates an outcome indicating that the submitted data did not meet business requirements or validation rules.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="messages">Explanations detailing the required corrections.</param>
    /// <returns>An invalid request result containing guidance messages.</returns>
    public static TResult CreateBadRequest<TResult>(List<string> messages) where TResult : ResultBase, new()
        => new() { StatusCode = StatusCode.BadRequest, Messages = messages, Success = false };

    /// <summary>
    /// Creates an outcome indicating an unexpected operational interruption.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="message">Guidance or context for support teams.</param>
    /// <returns>An unexpected failure result.</returns>
    public static TResult CreateUnknownError<TResult>(string message) where TResult : ResultBase, new()
        => new() { StatusCode = StatusCode.UnknownError, Messages = [message], Success = false };

    /// <summary>
    /// Creates a successful result associated with a specific business status.
    /// </summary>
    /// <param name="statusCode">The target business status code.</param>
    /// <returns>A successful result.</returns>
    public static Result CreateSuccess(StatusCode statusCode) => new() { StatusCode = statusCode, Success = true };

    /// <summary>
    /// Creates an error result associated with a specific business status.
    /// </summary>
    /// <param name="statusCode">The target status code.</param>
    /// <returns>An error result.</returns>
    public static Result CreateError(StatusCode statusCode) => new() { StatusCode = statusCode, Success = false };

    /// <summary>
    /// Creates an error result accompanied by a contextual explanation.
    /// </summary>
    /// <param name="statusCode">The target status code.</param>
    /// <param name="message">Contextual explanation or guidance.</param>
    /// <returns>A detailed error result.</returns>
    public static Result CreateError(StatusCode statusCode, string? message) => new()
    {
        StatusCode = statusCode,
        Messages = message is null ? [] : [message]
    };

    /// <summary>
    /// Converts a standard result into a specialized result model while preserving messages and status.
    /// </summary>
    /// <typeparam name="TResult">Target specialized result type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <returns>A new instance with matching status and messages.</returns>
    public static TResult Create<TResult>(Result result) where TResult : ResultBase, new()
        => new() { Success = result.Success, StatusCode = result.StatusCode, Messages = result.Messages ?? [] };

    /// <summary>
    /// Converts a specialized result into the standard system result format.
    /// </summary>
    /// <typeparam name="TResult">Source specialized result type.</typeparam>
    /// <param name="result">The original specialized result.</param>
    /// <returns>A standard result representation.</returns>
    public static Result Create<TResult>(TResult result) where TResult : ResultBase, new()
        => new() { Success = result.Success, StatusCode = result.StatusCode, Messages = result.Messages ?? [] };

    /// <summary>
    /// Creates a standard confirmation that the operation finished successfully.
    /// </summary>
    /// <returns>A successful 200 OK outcome.</returns>
    public static Result CreateSuccessOk() => CreateSuccess(StatusCode.OK);

    /// <summary>
    /// Creates a confirmation indicating that a new business record or resource was successfully registered.
    /// </summary>
    /// <returns>A creation confirmation outcome.</returns>
    public static Result CreateSuccessCreated() => CreateSuccess(StatusCode.Created);

    /// <summary>
    /// Creates a confirmation that an action finished with no additional content required.
    /// </summary>
    /// <returns>A no-content success outcome.</returns>
    public static Result CreateNoContent() => CreateSuccess(StatusCode.NoContent);

    /// <summary>
    /// Creates an outcome indicating that provided information violates validation requirements or business rules.
    /// </summary>
    /// <param name="message">Guidance on how to satisfy requirements.</param>
    /// <returns>A bad request outcome.</returns>
    public static Result CreateBadRequest(string message) => CreateError(StatusCode.BadRequest, message);

    /// <summary>
    /// Creates an outcome indicating that the action requires valid user authentication or authorization.
    /// </summary>
    /// <returns>An unauthorized outcome.</returns>
    public static Result CreateUnauthorized() => CreateError(StatusCode.Unauthorized);

    /// <summary>
    /// Creates an outcome indicating that the action requires authorization, including guidance.
    /// </summary>
    /// <param name="message">Authorization guidance.</param>
    /// <returns>An unauthorized outcome with instructions.</returns>
    public static Result CreateUnauthorized(string message) => CreateError(StatusCode.Unauthorized, message);

    /// <summary>
    /// Creates an outcome indicating that the requested information or record could not be found.
    /// </summary>
    /// <param name="message">Context describing the missing record.</param>
    /// <returns>A not found outcome.</returns>
    public static Result CreateNotFound(string? message = null) => CreateError(StatusCode.NotFound, message);

    /// <summary>
    /// Creates an outcome indicating that the operation timed out before completing.
    /// </summary>
    /// <param name="message">Explanation of the elapsed time limit.</param>
    /// <returns>A timeout outcome.</returns>
    public static Result CreateRequestTimeout(string message) => CreateError(StatusCode.RequestTimeout, message);

    /// <summary>
    /// Creates an outcome indicating that the request was understood but cannot be processed under current business rules.
    /// </summary>
    /// <param name="message">Specific business constraint that prevented processing.</param>
    /// <returns>An unprocessable entity outcome.</returns>
    public static Result CreateUnprocessableEntity(string message) => CreateError(StatusCode.UnprocessableEntity, message);

    /// <summary>
    /// Creates an outcome indicating an unexpected operational failure.
    /// </summary>
    /// <returns>An unknown error outcome.</returns>
    public static Result CreateUnknownError() => CreateError(StatusCode.UnknownError);

    /// <summary>
    /// Creates an outcome indicating an unexpected operational failure with descriptive guidance.
    /// </summary>
    /// <param name="message">Contextual failure message.</param>
    /// <returns>An unknown error outcome.</returns>
    public static Result CreateUnknownError(string message) => CreateError(StatusCode.UnknownError, message);

    /// <summary>
    /// Creates an outcome indicating temporary unavailability of a business service or integration.
    /// </summary>
    /// <param name="message">Status and retry guidance.</param>
    /// <returns>A service unavailable outcome.</returns>
    public static Result CreateServiceUnavailable(string message) => CreateError(StatusCode.ServiceUnavailable, message);

    /// <summary>
    /// Creates a successful outcome delivering business data with a specific status code.
    /// </summary>
    /// <typeparam name="TResultData">The type of business data payload.</typeparam>
    /// <param name="statusCode">Status of the completed operation.</param>
    /// <param name="resultData">The business payload delivered to the client.</param>
    /// <returns>A successful outcome carrying data.</returns>
    public static Result<TResultData> CreateSuccess<TResultData>(StatusCode statusCode, TResultData? resultData = default)
        => new() { StatusCode = statusCode, Data = resultData, Success = true };

    /// <summary>
    /// Creates a standard successful outcome delivering business data.
    /// </summary>
    /// <typeparam name="TResultData">The type of business data payload.</typeparam>
    /// <param name="resultData">The business payload delivered to the client.</param>
    /// <returns>A successful 200 OK outcome carrying data.</returns>
    public static Result<TResultData> CreateSuccessOk<TResultData>(TResultData? resultData = default)
        => CreateSuccess(StatusCode.OK, resultData);

    /// <summary>
    /// Creates an outcome confirming that a new record was created, delivering the created record details.
    /// </summary>
    /// <typeparam name="TResultData">The type of business data payload.</typeparam>
    /// <param name="resultData">The newly registered business item.</param>
    /// <returns>A creation confirmation outcome carrying data.</returns>
    public static Result<TResultData> CreateSuccessCreated<TResultData>(TResultData resultData)
        => CreateSuccess(StatusCode.Created, resultData);

    /// <summary>
    /// Creates an outcome indicating that the action conflicts with existing records (e.g. duplicate key).
    /// </summary>
    /// <typeparam name="TResultData">The type of business data payload.</typeparam>
    /// <param name="data">The conflicting business item.</param>
    /// <returns>A conflict outcome.</returns>
    public static Result<TResultData> CreateConflict<TResultData>(TResultData data)
        => new() { Data = data, StatusCode = StatusCode.Conflict, Success = false };

    /// <summary>
    /// Creates a conflict outcome with an explanatory message.
    /// </summary>
    /// <typeparam name="TResultData">The type of business data payload.</typeparam>
    /// <param name="data">The conflicting business item.</param>
    /// <param name="message">Explanation of the duplication or conflict.</param>
    /// <returns>A conflict outcome with descriptive messages.</returns>
    public static Result<TResultData> CreateConflict<TResultData>(TResultData data, string message)
        => new() { Data = data, Messages = [message], StatusCode = StatusCode.Conflict, Success = false };

    /// <summary>
    /// Creates a successful outcome delivering a list of business records.
    /// </summary>
    /// <typeparam name="TResultData">The type of items in the list.</typeparam>
    /// <param name="statusCode">Status of the completed operation.</param>
    /// <param name="data">The list of business items returned.</param>
    /// <returns>A successful listed outcome.</returns>
    public static ListedResult<TResultData> CreateListedSuccess<TResultData>(StatusCode statusCode, IEnumerable<TResultData>? data = null)
        => new() { Data = data?.ToList(), StatusCode = statusCode, Success = true };

    /// <summary>
    /// Creates a standard successful outcome delivering a list of business records.
    /// </summary>
    /// <typeparam name="TResultData">The type of items in the list.</typeparam>
    /// <param name="data">The list of business items returned.</param>
    /// <returns>A successful 200 OK listed outcome.</returns>
    public static ListedResult<TResultData> CreateListedSuccessOk<TResultData>(IEnumerable<TResultData>? data = null)
        => CreateListedSuccess(StatusCode.OK, data);

    /// <summary>
    /// Creates a successful paginated outcome tailored for tabular business reports and user interfaces.
    /// </summary>
    /// <typeparam name="TResultData">The type of items in the page.</typeparam>
    /// <param name="data">The items belonging to the current page.</param>
    /// <returns>A paginated outcome.</returns>
    public static PagedResult<TResultData> CreatePagedSuccessOk<TResultData>(IEnumerable<TResultData>? data = null)
        => new() { StatusCode = StatusCode.OK, Data = data?.ToList() };
}

/// <summary>
/// Represents the outcome of a business operation that returns a specific value or report to the caller.
/// </summary>
/// <typeparam name="TResultData">Type of the business payload delivered.</typeparam>
public record Result<TResultData> : ResultBase
{
    /// <summary>
    /// The business payload or document produced by the operation.
    /// </summary>
    public TResultData? Data { get; init; }

    /// <summary>
    /// Transforms the payload while preserving original business status and feedback messages.
    /// </summary>
    /// <typeparam name="T">New payload type.</typeparam>
    /// <param name="newData">The transformed business data.</param>
    /// <returns>A new result with the updated payload.</returns>
    public Result<T> ChangeData<T>(T newData) => Data is not null
        ? new Result<T> { Data = newData, Messages = Messages, StatusCode = StatusCode, Success = Success }
        : Result.Create(this);

    /// <summary>
    /// Converts a basic result into a typed data result.
    /// </summary>
    public static implicit operator Result<TResultData>(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };

    /// <summary>
    /// Automatically converts a business value into a successful result.
    /// </summary>
    public static implicit operator Result<TResultData>(TResultData resultData) => Result.CreateSuccessOk(resultData);
}

/// <summary>
/// Represents the outcome of a business operation that returns a collection of items.
/// </summary>
/// <typeparam name="TResultData">Type of each item in the collection.</typeparam>
public record ListedResult<TResultData> : ResultBase
{
    /// <summary>
    /// Collection of business items returned.
    /// </summary>
    public List<TResultData>? Data { get; init; }

    /// <summary>
    /// Total count of items delivered in this collection.
    /// </summary>
    public long Count => Data?.Count ?? 0;

    /// <summary>
    /// Implicitly converts a general result into a listed result structure.
    /// </summary>
    public static implicit operator ListedResult<TResultData>(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}

/// <summary>
/// Represents the outcome of queries split across pages to optimize report display and browsing speed.
/// </summary>
/// <typeparam name="TResultData">Type of each item in the page.</typeparam>
public record PagedResult<TResultData> : ListedResult<TResultData>
{
    /// <summary>
    /// Total number of items matching the query criteria across all pages.
    /// </summary>
    public long TotalCount { get; init; }

    /// <summary>
    /// 1-based index of the currently viewed page.
    /// </summary>
    public long PageNumber { get; init; }

    /// <summary>
    /// Maximum count of items displayed per page.
    /// </summary>
    public long PageSize { get; init; }

    /// <summary>
    /// Implicitly converts a general result into a paginated result structure.
    /// </summary>
    public static implicit operator PagedResult<TResultData>(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}
