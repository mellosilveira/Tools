using System.Net;

namespace MelloSilveiraTools.DataContracts.Operations;

/// <summary>
/// Response content for all operations.
/// </summary>
public record OperationResponse
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResponse"/>.
    /// </summary>
    protected OperationResponse()
    {
        ErrorMessages = [];
    }

    protected OperationResponse(HttpStatusCode statusCode, bool success) : this()
    {
        HttpStatusCode = statusCode;
        Success = success;
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

    #region Basic methods to add error.

    /// <summary>
    /// This method adds an error message to response.
    /// </summary>
    /// <param name="errorMessage"></param>
    /// <param name="httpStatusCode"></param>
    public void AddError(string errorMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        ErrorMessages.Add(errorMessage);
        HttpStatusCode = httpStatusCode;
        Success = false;
    }

    /// <summary>
    /// This method adds multiple error messages.
    /// </summary>
    /// <param name="errorMessages"></param>
    /// <param name="httpStatusCode"></param>
    public void AddErrors(List<string> errorMessages, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        ErrorMessages.AddRange(errorMessages);
        HttpStatusCode = httpStatusCode;
        Success = false;
    }

    /// <summary>
    /// This method adds multiple error messages.
    /// </summary>
    /// <param name="response"></param>
    public void AddErrors(OperationResponse response)
    {
        ErrorMessages.AddRange(response.ErrorMessages);
        HttpStatusCode = response.HttpStatusCode;
        Success = false;
    }

    /// <summary>
    /// This method adds error by a condition expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="errorMessage"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIf(Func<bool> expression, string errorMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        if (expression())
            AddError(errorMessage, httpStatusCode);

        return this;
    }

    #endregion

    #region Methods to add error based on a condition.

    /// <summary>
    /// This method adds error if the object is null.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="parameterName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfNull(object value, string parameterName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        return AddErrorIf(() => value is null, $"'{parameterName}' cannot be null.", httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the object is null.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="parameterName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfNotNull(object value, string parameterName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        return AddErrorIf(() => value != null, $"'{parameterName}' must be null.", httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the value is null or empty.
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="parameterName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfNullOrEmpty(string parameter, string parameterName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        return AddErrorIf(() => string.IsNullOrEmpty(parameter), $"'{parameterName}' cannot be null or empty.", httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the value is negative or equal to zero.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="parametersName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfNullOrEmpty<T>(IEnumerable<T> parameters, string parametersName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        return AddErrorIf(() => parameters is null || !parameters.Any(), $"'{parametersName}' cannot be null or empty.", httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the value is zero.
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="parameterName"></param>
    /// <param name="additionalMessage"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfZero(double parameter, string parameterName, string additionalMessage = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        string errorMessage = $"The '{parameterName}' cannot be zero.";

        if (additionalMessage != null)
            errorMessage = $"{additionalMessage} {errorMessage}";

        return AddErrorIf(() => parameter == 0, errorMessage, httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the value is negative.
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="parameterName"></param>
    /// <param name="additionalMessage"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfNegative(double parameter, string parameterName, string additionalMessage = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        string errorMessage = $"The '{parameterName}' cannot be negative.";

        if (additionalMessage != null)
            errorMessage = $"{additionalMessage} {errorMessage}";

        return AddErrorIf(() => parameter < 0, errorMessage, httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the value is negative or equal to zero.
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="parameterName"></param>
    /// <param name="additionalMessage"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfNegativeOrZero(double parameter, string parameterName, string additionalMessage = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        string errorMessage = $"The '{parameterName}' cannot be negative or equal to zero.";
        if (additionalMessage != null)
            errorMessage = $"{additionalMessage} {errorMessage}";

        return AddErrorIf(() => parameter <= 0, errorMessage, httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the value is negative or equal to zero.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="parametersName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfNegativeOrZero(List<double> parameters, string parametersName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        AddErrorIfNullOrEmpty(parameters, parametersName, httpStatusCode);
        if (!Success)
            return this;

        foreach (double parameter in parameters)
        {
            AddErrorIfNegativeOrZero(parameter, parametersName, $"Index {parameters.IndexOf(parameter)}.", httpStatusCode);
        }

        return this;
    }

    /// <summary>
    /// This method adds error if the enum is invalid.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="message"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfInvalidEnum<TEnum>(TEnum value, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        where TEnum : struct, Enum
    {
        return AddErrorIf(() => !Enum.IsDefined(value), message, httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the file does not exist.
    /// </summary>
    /// <param name="fullFileName"></param>
    /// <param name="parameterName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfFileNotExist(string fullFileName, string parameterName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        AddErrorIfNullOrEmpty(fullFileName, parameterName, httpStatusCode);
        if (!Success)
            return this;

        FileInfo fileInfo = new(fullFileName);
        return AddErrorIf(() => !fileInfo.Exists, $"File '{fullFileName}' does not exist.", httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the file already exists.
    /// </summary>
    /// <param name="fullFileName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfFileExist(string fullFileName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        FileInfo fileInfo = new(fullFileName);
        return AddErrorIf(() => fileInfo.Exists, $"File '{fullFileName}' already exists.", httpStatusCode);
    }

    /// <summary>
    /// This method adds error if the directory does not exist.
    /// </summary>
    /// <param name="fullDirectoryName"></param>
    /// <param name="parameterName"></param>
    /// <param name="httpStatusCode"></param>
    /// <returns></returns>
    public OperationResponse AddErrorIfDirectoryNotExist(string fullDirectoryName, string parameterName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
    {
        AddErrorIfNullOrEmpty(fullDirectoryName, parameterName, httpStatusCode);
        if (!Success)
            return this;

        DirectoryInfo directoryInfo = new(fullDirectoryName);
        return AddErrorIf(() => !directoryInfo.Exists, $"Directory '{fullDirectoryName}' does not exist.", httpStatusCode);
    }

    #endregion

    #region Methods to set the HTTP Status Code in case of sucess

    /// <summary>
    /// This method sets Success to true and the HttpStatusCode to 200 (OK).
    /// </summary>
    public void SetSuccessOk() => SetSuccess(HttpStatusCode.OK);

    /// <summary>
    /// This method sets Success to true and the HttpStatusCode to 201 (Created).
    /// </summary>
    public void SetSuccessCreated() => SetSuccess(HttpStatusCode.Created);

    /// <summary>
    /// This method sets Success to true and the HttpStatusCode to 202 (Accepted).
    /// </summary>
    public void SetSuccessAccepted() => SetSuccess(HttpStatusCode.Accepted);

    /// <summary>
    /// This method sets Success to true and the HttpStatusCode to 206 (PartialContent).
    /// </summary>
    public void SetSuccessPartialContent() => SetSuccess(HttpStatusCode.PartialContent);

    /// <summary>
    /// This method sets Sucess to true.
    /// </summary>
    /// <param name="httpStatusCode"></param>
    protected static OperationResponse SetSuccess(HttpStatusCode httpStatusCode)
    {
        
        HttpStatusCode = httpStatusCode;
        Success = true;
    }

    #endregion

    #region Methods to set the HTTP Status Code in case of error

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 400 (BadRequest).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetBadRequestError(string errorMessage = null) => SetError(HttpStatusCode.BadRequest, errorMessage);

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 401 (Unauthorized).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetUnauthorizedError(string errorMessage = null) => SetError(HttpStatusCode.Unauthorized, errorMessage);

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 404 (NotFound).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetNotFoundError(string errorMessage = null) => SetError(HttpStatusCode.NotFound, errorMessage);

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 409 (Conflict).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetConflictError(string errorMessage = null) => SetError(HttpStatusCode.Conflict, errorMessage);

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 413 (RequestEntityTooLarge).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetRequestEntityTooLargeError(string errorMessage = null) => SetError(HttpStatusCode.RequestEntityTooLarge, errorMessage);

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 422 (UnprocessableEntity).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetUnprocessableEntityError(string errorMessage = null) => SetError(HttpStatusCode.UnprocessableEntity, errorMessage);

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 500 (InternalServerError).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetInternalServerError(string errorMessage = null) => SetError(HttpStatusCode.InternalServerError, errorMessage);

    /// <summary>
    /// This method sets Success to false and the HttpStatusCode to 501 (NotImplemented).
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SetNotImplementedError(string errorMessage = null) => SetError(HttpStatusCode.NotImplemented, errorMessage);

    /// <summary>
    /// This method sets Success to false.
    /// </summary>
    /// <param name="httpStatusCode"></param>
    /// <param name="errorMessage"></param>
    protected void SetError(HttpStatusCode httpStatusCode, string errorMessage = null)
    {
        if (errorMessage != null)
            ErrorMessages.Add(errorMessage);

        HttpStatusCode = httpStatusCode;
        Success = false;
    }

    #endregion
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
