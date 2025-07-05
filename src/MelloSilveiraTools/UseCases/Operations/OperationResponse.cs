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

    public static OperationResponse CreateSuccessOk() => new() { StatusCode = HttpStatusCode.OK };

    public static T CreateSuccessOk<T>() where T : OperationResponse, new() => new() { StatusCode = HttpStatusCode.OK };

    public static OperationResponse CreateInternalServerError(string message) => new()
    {
        StatusCode = HttpStatusCode.InternalServerError,
        ErrorMessages = [message]
    };

    public static T CreateInternalServerError<T>(string message) where T : OperationResponse, new() => new()
    {
        StatusCode = HttpStatusCode.InternalServerError,
        ErrorMessages = [message]
    };
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
    public TResponseData? Data { get; protected set; }

    public void SetData(TResponseData data) => Data = data;

    public static TResponse CreateSuccessOk<TResponse>(TResponseData? data = null) where TResponse : OperationResponseBase<TResponseData>, new() => new()
    {
         Data = data,
        StatusCode = HttpStatusCode.OK,
    };
}
