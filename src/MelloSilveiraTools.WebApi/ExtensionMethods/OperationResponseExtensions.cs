using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.WebApi.Application.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="OperationResponseBase"/>.
/// </summary>
public static class OperationResponseExtensions
{
    /// <summary>
    /// Appends <paramref name="errorMessage"/> to the response and marks it as failed with the given status code.
    /// </summary>
    public static T AddError<T>(this T response, string errorMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase
    {
        response.Messages.Add(errorMessage);
        return response with { StatusCode = httpStatusCode, Success = false };
    }

    /// <summary>
    /// Appends <paramref name="errorMessage"/> to the response only when <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    public static T AddErrorIf<T>(this T response, bool condition, string errorMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase => condition
        ? response.AddError(errorMessage, httpStatusCode)
        : response;

    /// <summary>
    /// Awaits the condition task and, when it resolves to <c>true</c>, appends <paramref name="errorMessage"/> to the response.
    /// </summary>
    public static async Task<T> AddErrorIf<T>(this T response, Task<bool> condition, string errorMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase => await condition
        ? response.AddError(errorMessage, httpStatusCode)
        : response;

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="value"/> is <c>null</c>.
    /// </summary>
    public static T AddErrorIfNull<T>(this T response, object value, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(value is null, message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="value"/> is not <c>null</c>.
    /// </summary>
    public static T AddErrorIfNotNull<T>(this T response, object value, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(value is not null, message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is <c>null</c> or empty.
    /// </summary>
    public static T AddErrorIfNullOrEmpty<T>(this T response, string parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(string.IsNullOrEmpty(parameter), message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is <c>null</c>, empty, or whitespace.
    /// </summary>
    public static T AddErrorIfNullOrWhiteSpace<T>(this T response, string parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(string.IsNullOrWhiteSpace(parameter), message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameters"/> is <c>null</c> or contains no elements.
    /// </summary>
    public static T AddErrorIfNullOrEmpty<T, TSource>(this T response, IEnumerable<TSource> parameters, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(parameters.IsNullOrEmpty(), message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> equals zero.
    /// </summary>
    public static T AddErrorIfZero<T>(this T response, double parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(parameter == 0, message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is strictly negative.
    /// </summary>
    public static T AddErrorIfNegative<T>(this T response, double parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(parameter < 0, message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is negative or zero.
    /// </summary>
    public static T AddErrorIfNegativeOrZero<T>(this T response, double parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase 
        => response.AddErrorIf(parameter <= 0, message, httpStatusCode);

    /// <summary>
    /// Validates each element of <paramref name="parameters"/> and appends <paramref name="message"/> when any value is negative or zero,
    /// or when the list itself is <c>null</c> or empty.
    /// </summary>
    public static T AddErrorIfNegativeOrZero<T>(this T response, List<double> parameters, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase
    {
        response.AddErrorIfNullOrEmpty(parameters, message, httpStatusCode);
        if (!response.Success)
            return response;

        foreach (double parameter in parameters)
        {
            response.AddErrorIfNegativeOrZero(parameter, message, httpStatusCode);
        }

        return response;
    }

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="value"/> is not a defined member of the enum type.
    /// </summary>
    public static T AddErrorIfInvalidEnum<T, TEnum>(this T response, TEnum value, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase where TEnum : struct, Enum 
        => response.AddErrorIf(!Enum.IsDefined(value), message, httpStatusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when the file does not exist or when <paramref name="fullFileName"/> is blank.
    /// </summary>
    public static T AddErrorIfFileNotExist<T>(this T response, string fullFileName, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase
    {
        response.AddErrorIfNullOrWhiteSpace(fullFileName, message, httpStatusCode);
        if (!response.Success)
            return response;

        FileInfo fileInfo = new(fullFileName);
        return response.AddErrorIf(!fileInfo.Exists, message, httpStatusCode);
    }

    /// <summary>
    /// Appends an error when the file at <paramref name="fullFileName"/> already exists.
    /// </summary>
    public static T AddErrorIfFileExist<T>(this T response, string fullFileName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase
    {
        FileInfo fileInfo = new(fullFileName);
        return response.AddErrorIf(fileInfo.Exists, $"File '{fullFileName}' already exists.", httpStatusCode);
    }

    /// <summary>
    /// Appends an error when the directory does not exist or when <paramref name="fullDirectoryName"/> is blank.
    /// </summary>
    public static T AddErrorIfDirectoryNotExist<T>(this T response, string fullDirectoryName, string parameterName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponseBase
    {
        response.AddErrorIfNullOrEmpty(fullDirectoryName, parameterName, httpStatusCode);
        if (!response.Success)
            return response;

        DirectoryInfo directoryInfo = new(fullDirectoryName);
        return response.AddErrorIf(!directoryInfo.Exists, $"Directory '{fullDirectoryName}' does not exist.", httpStatusCode);
    }

    /// <summary>
    /// Projects the operation response into a <see cref="JsonResult"/> carrying the same HTTP status code.
    /// </summary>
    public static JsonResult BuildHttpResponse<T>(this T response) where T : OperationResponseBase
        => new(response) { StatusCode = (int)response.StatusCode };

    /// <summary>
    /// Awaits the response task and projects the result into a <see cref="JsonResult"/> carrying the same HTTP status code.
    /// </summary>
    /// <param name="responseTask">The task containing the operation response to be returned.</param>
    /// <returns>A <see cref="JsonResult"/> that serializes the operation response with the response status code.</returns>
    public static async Task<JsonResult> BuildHttpResponseAsync<T>(this Task<T> responseTask) where T : OperationResponseBase
    {
        var response = await responseTask.ConfigureAwait(false);
        return response.BuildHttpResponse();
    }

    /// <summary>
    /// Projects the operation response into an <see cref="IResult"/> suitable for minimal-API endpoints,
    /// preserving the response payload and HTTP status code.
    /// </summary>
    /// <param name="responseTask">The task containing the operation response to be returned.</param>
    /// <returns>An <see cref="IResult"/> that serializes operation response as JSON with the response status code.</returns>
    public static async Task<IResult> ToHttpResultAsync<T>(this Task<T> responseTask) where T : OperationResponseBase
    {
        var response = await responseTask.ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Projects the operation response into an <see cref="IResult"/> suitable for minimal-API endpoints,
    /// preserving the response payload and HTTP status code.
    /// </summary>
    /// <param name="response">The operation response to be returned.</param>
    /// <returns>An <see cref="IResult"/> that serializes <paramref name="response"/> as JSON with the response status code.</returns>
    public static IResult ToHttpResult<T>(this T response) where T : OperationResponseBase
        => Results.Json(response, statusCode: (int)response.StatusCode);
}
