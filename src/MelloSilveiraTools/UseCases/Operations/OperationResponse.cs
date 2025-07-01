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
    public bool Success { get; init; }

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public HttpStatusCode HttpStatusCode { get; init; }

    /// <summary>
    /// The list of error message.
    /// </summary>
    public List<string> ErrorMessages { get; init; }

    #region Methods to set the HTTP Status Code in case of sucess

    ///// <summary>
    ///// This method sets Success to true and the HttpStatusCode to 200 (OK).
    ///// </summary>
    //public void SetSuccessOk() => SetSuccess(HttpStatusCode.OK);

    ///// <summary>
    ///// This method sets Success to true and the HttpStatusCode to 201 (Created).
    ///// </summary>
    //public void SetSuccessCreated() => SetSuccess(HttpStatusCode.Created);

    ///// <summary>
    ///// This method sets Success to true and the HttpStatusCode to 202 (Accepted).
    ///// </summary>
    //public void SetSuccessAccepted() => SetSuccess(HttpStatusCode.Accepted);

    ///// <summary>
    ///// This method sets Success to true and the HttpStatusCode to 206 (PartialContent).
    ///// </summary>
    //public void SetSuccessPartialContent() => SetSuccess(HttpStatusCode.PartialContent);

    ///// <summary>
    ///// This method sets Sucess to true.
    ///// </summary>
    ///// <param name="httpStatusCode"></param>
    //protected void SetSuccess(HttpStatusCode httpStatusCode)
    //{
    //    HttpStatusCode = httpStatusCode;
    //    Success = true;
    //}

    #endregion

    #region Methods to set the HTTP Status Code in case of error

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 400 (BadRequest).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetBadRequestError(string errorMessage = null) => SetError(HttpStatusCode.BadRequest, errorMessage);

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 401 (Unauthorized).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetUnauthorizedError(string errorMessage = null) => SetError(HttpStatusCode.Unauthorized, errorMessage);

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 404 (NotFound).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetNotFoundError(string errorMessage = null) => SetError(HttpStatusCode.NotFound, errorMessage);

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 409 (Conflict).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetConflictError(string errorMessage = null) => SetError(HttpStatusCode.Conflict, errorMessage);

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 413 (RequestEntityTooLarge).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetRequestEntityTooLargeError(string errorMessage = null) => SetError(HttpStatusCode.RequestEntityTooLarge, errorMessage);

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 422 (UnprocessableEntity).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetUnprocessableEntityError(string errorMessage = null) => SetError(HttpStatusCode.UnprocessableEntity, errorMessage);

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 500 (InternalServerError).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetInternalServerError(string errorMessage = null) => SetError(HttpStatusCode.InternalServerError, errorMessage);

    ///// <summary>
    ///// This method sets Success to false and the HttpStatusCode to 501 (NotImplemented).
    ///// </summary>
    ///// <param name="errorMessage"></param>
    //public void SetNotImplementedError(string errorMessage = null) => SetError(HttpStatusCode.NotImplemented, errorMessage);

    ///// <summary>
    ///// This method sets Success to false.
    ///// </summary>
    ///// <param name="httpStatusCode"></param>
    ///// <param name="errorMessage"></param>
    //protected void SetError(HttpStatusCode httpStatusCode, string errorMessage = null)
    //{
    //    if (errorMessage != null)
    //        ErrorMessages.Add(errorMessage);

    //    HttpStatusCode = httpStatusCode;
    //    Success = false;
    //}

    #endregion

    public static OperationResponse CreateWithSuccessOk() => new()
    {
        HttpStatusCode = HttpStatusCode.OK,
        Success = true
    };

    public static T CreateWithSuccessOk<T>() where T : OperationResponse, new() => new()
    {
        HttpStatusCode = HttpStatusCode.OK,
        Success = true
    };

    public static OperationResponse CreateWithInternalServerError(string message) => new()
    {
        HttpStatusCode = HttpStatusCode.InternalServerError,
        Success = false,
        ErrorMessages = [message]
    };

    public static T CreateWithInternalServerError<T>(string message) where T : OperationResponse, new() => new()
    {
        HttpStatusCode = HttpStatusCode.InternalServerError,
        Success = false,
        ErrorMessages = [message]
    };
}

/// <summary>
/// Response content for all operations.
/// </summary>
/// <typeparam name="TResponseData"></typeparam>
public record OperationResponseBase<TResponseData> : OperationResponse
{
    /// <summary>
    /// Data content of all operation response.
    /// </summary>
    public TResponseData Data { get; set; }
}
