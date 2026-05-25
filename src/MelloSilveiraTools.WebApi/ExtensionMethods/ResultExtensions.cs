using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Result"/>.
/// </summary>
public static class ResultExtensions
{
    public static T OnError<T>(this T result, Func<T> action) where T : ResultBase
        => result.Success ? result : action();

    public static Task<T> OnError<T>(this T result, Func<Task<T>> action) where T : ResultBase
        => result.Success ? Task.FromResult(result) : action();

    public static T OnSuccess<T>(this T result, Func<T> action) where T : ResultBase
        => result.Success ? action() : result;

    public static Task<T> OnSuccess<T>(this T result, Func<Task<T>> action) where T : ResultBase
        => result.Success ? action() : Task.FromResult(result);

    public static TOut Match<TIn, TOut>(this TIn result, Func<TOut> onSuccess, Func<TIn, TOut> onError)
        where TIn : Result
        where TOut : Result
        => result.Success ? onSuccess() : onError(result);

    public static Task<TOut> Match<TIn, TOut>(this TIn? result, Func<Task<TOut>> onSuccess, Func<TIn, TOut> onError)
        where TIn : Result
        where TOut : Result
        => result is null || result.Success ? onSuccess() : Task.FromResult(onError(result));

    public static Task<T> Match<T>(this T result, Func<Task<T>> onSuccess, Func<Task<T>> onError) where T : ResultBase
        => result.Success ? onSuccess() : onError();

    /// <summary>
    /// Appends <paramref name="errorMessage"/> to the result and marks it as failed with the given status code.
    /// </summary>
    public static T AddError<T>(this T result, string errorMessage, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
    {
        result.Messages.Add(errorMessage);
        return result with { StatusCode = statusCode, Success = false };
    }

    /// <summary>
    /// Appends <paramref name="errorMessage"/> to the result only when <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    public static T AddErrorIf<T>(this T result, bool condition, string errorMessage, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase => condition
        ? result.AddError(errorMessage, statusCode)
        : result;

    public static T AddErrorIf<T>(this T result, bool condition, Func<T, T> errorFunc) where T : ResultBase => condition ? errorFunc(result) : result;

    /// <summary>
    /// Awaits the condition task and, when it resolves to <c>true</c>, appends <paramref name="errorMessage"/> to the result.
    /// </summary>
    public static async Task<T> AddErrorIf<T>(this T result, Task<bool> condition, string errorMessage, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase => await condition
        ? result.AddError(errorMessage, statusCode)
        : result;

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="value"/> is <c>null</c>.
    /// </summary>
    public static T AddErrorIfNull<T>(this T result, object value, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(value is null, message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="value"/> is not <c>null</c>.
    /// </summary>
    public static T AddErrorIfNotNull<T>(this T result, object value, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(value is not null, message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is <c>null</c> or empty.
    /// </summary>
    public static T AddErrorIfNullOrEmpty<T>(this T result, string parameter, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(string.IsNullOrEmpty(parameter), message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is <c>null</c>, empty, or whitespace.
    /// </summary>
    public static T AddErrorIfNullOrWhiteSpace<T>(this T result, string parameter, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(string.IsNullOrWhiteSpace(parameter), message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameters"/> is <c>null</c> or contains no elements.
    /// </summary>
    public static T AddErrorIfNullOrEmpty<T, TSource>(this T result, IEnumerable<TSource> parameters, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(parameters.IsNullOrEmpty(), message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> equals zero.
    /// </summary>
    public static T AddErrorIfZero<T>(this T result, double parameter, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(parameter == 0, message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is strictly negative.
    /// </summary>
    public static T AddErrorIfNegative<T>(this T result, double parameter, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(parameter < 0, message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="parameter"/> is negative or zero.
    /// </summary>
    public static T AddErrorIfNegativeOrZero<T>(this T result, double parameter, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
        => result.AddErrorIf(parameter <= 0, message, statusCode);

    /// <summary>
    /// Validates each element of <paramref name="parameters"/> and appends <paramref name="message"/> when any value is negative or zero,
    /// or when the list itself is <c>null</c> or empty.
    /// </summary>
    public static T AddErrorIfNegativeOrZero<T>(this T result, List<double> parameters, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
    {
        result.AddErrorIfNullOrEmpty(parameters, message, statusCode);
        if (!result.Success)
            return result;

        foreach (double parameter in parameters)
        {
            result.AddErrorIfNegativeOrZero(parameter, message, statusCode);
        }

        return result;
    }

    /// <summary>
    /// Appends <paramref name="message"/> when <paramref name="value"/> is not a defined member of the enum type.
    /// </summary>
    public static T AddErrorIfInvalidEnum<T, TEnum>(this T result, TEnum value, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase where TEnum : struct, Enum
        => result.AddErrorIf(!Enum.IsDefined(value), message, statusCode);

    /// <summary>
    /// Appends <paramref name="message"/> when the file does not exist or when <paramref name="fullFileName"/> is blank.
    /// </summary>
    public static T AddErrorIfFileNotExist<T>(this T result, string fullFileName, string message, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
    {
        result.AddErrorIfNullOrWhiteSpace(fullFileName, message, statusCode);
        if (!result.Success)
            return result;

        FileInfo fileInfo = new(fullFileName);
        return result.AddErrorIf(!fileInfo.Exists, message, statusCode);
    }

    /// <summary>
    /// Appends an error when the file at <paramref name="fullFileName"/> already exists.
    /// </summary>
    public static T AddErrorIfFileExist<T>(this T result, string fullFileName, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
    {
        FileInfo fileInfo = new(fullFileName);
        return result.AddErrorIf(fileInfo.Exists, $"File '{fullFileName}' already exists.", statusCode);
    }

    /// <summary>
    /// Appends an error when the directory does not exist or when <paramref name="fullDirectoryName"/> is blank.
    /// </summary>
    public static T AddErrorIfDirectoryNotExist<T>(this T result, string fullDirectoryName, string parameterName, StatusCode statusCode = StatusCode.BadRequest) where T : ResultBase
    {
        result.AddErrorIfNullOrEmpty(fullDirectoryName, parameterName, statusCode);
        if (!result.Success)
            return result;

        DirectoryInfo directoryInfo = new(fullDirectoryName);
        return result.AddErrorIf(!directoryInfo.Exists, $"Directory '{fullDirectoryName}' does not exist.", statusCode);
    }

    /// <summary>
    /// Projects the operation result into a <see cref="JsonResult"/> carrying the same HTTP status code.
    /// </summary>
    public static JsonResult BuildHttpResponse<T>(this T result) where T : ResultBase
        => new(result) { StatusCode = (int)result.StatusCode };

    /// <summary>
    /// Awaits the result task and projects the result into a <see cref="JsonResult"/> carrying the same HTTP status code.
    /// </summary>
    /// <param name="resultTask">The task containing the operation result to be returned.</param>
    /// <returns>A <see cref="JsonResult"/> that serializes the operation result with the result status code.</returns>
    public static async Task<JsonResult> BuildHttpResponseAsync<T>(this Task<T> resultTask) where T : ResultBase
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.BuildHttpResponse();
    }

    /// <summary>
    /// Projects the operation result into an <see cref="IResult"/> suitable for minimal-API endpoints,
    /// preserving the result payload and HTTP status code.
    /// </summary>
    /// <param name="resultTask">The task containing the operation result to be returned.</param>
    /// <returns>An <see cref="IResult"/> that serializes operation result as JSON with the result status code.</returns>
    public static async Task<IResult> ToHttpResultAsync<T>(this Task<T> resultTask) where T : ResultBase
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.ToHttpResult();
    }

    /// <summary>
    /// Projects the operation result into an <see cref="IResult"/> suitable for minimal-API endpoints,
    /// preserving the result payload and HTTP status code.
    /// </summary>
    /// <param name="result">The operation result to be returned.</param>
    /// <returns>An <see cref="IResult"/> that serializes <paramref name="result"/> as JSON with the result status code.</returns>
    public static IResult ToHttpResult<T>(this T result) where T : ResultBase
        => Results.Json(result, statusCode: (int)result.StatusCode);
}
