using MelloSilveiraTools.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Result"/>.
/// </summary>
public static class ResultExtensions
{
    extension<T>(T result) where T : ResultBase
    {
        public T OnError(Func<T> action) => result.Success ? result : action();

        public Task<T> OnError(Func<Task<T>> action) => result.Success ? Task.FromResult(result) : action();

        public T OnSuccess(Func<T> action) => result.Success ? action() : result;

        public Task<T> OnSuccess(Func<Task<T>> action) => result.Success ? action() : Task.FromResult(result);

        public Task<T> Match(Func<Task<T>> onSuccess, Func<Task<T>> onError) => result.Success ? onSuccess() : onError();

        /// <summary>
        /// Appends <paramref name="errorMessage"/> to the result and marks it as failed with the given status code.
        /// </summary>
        public T AddError(string errorMessage, StatusCode statusCode = StatusCode.BadRequest)
        {
            result.Messages.Add(errorMessage);
            return result with { StatusCode = statusCode, Success = false };
        }

        /// <summary>
        /// Appends <paramref name="errorMessage"/> to the result only when <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        public T AddErrorIf(bool condition, string errorMessage, StatusCode statusCode = StatusCode.BadRequest) => condition
            ? result.AddError(errorMessage, statusCode)
            : result;

        public T AddErrorIf(bool condition, Func<T, T> errorFunc) => condition ? errorFunc(result) : result;

        /// <summary>
        /// Awaits the condition task and, when it resolves to <c>true</c>, appends <paramref name="errorMessage"/> to the result.
        /// </summary>
        public async Task<T> AddErrorIf(Task<bool> condition, string errorMessage, StatusCode statusCode = StatusCode.BadRequest) => await condition.ConfigureAwait(false)
            ? result.AddError(errorMessage, statusCode)
            : result;

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="value"/> is <c>null</c>.
        /// </summary>
        public T AddErrorIfNull(object value, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(value is null, message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="value"/> is not <c>null</c>.
        /// </summary>
        public T AddErrorIfNotNull(object value, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(value is not null, message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="parameter"/> is <c>null</c> or empty.
        /// </summary>
        public T AddErrorIfNullOrEmpty(string parameter, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(string.IsNullOrEmpty(parameter), message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="parameter"/> is <c>null</c>, empty, or whitespace.
        /// </summary>
        public T AddErrorIfNullOrWhiteSpace(string parameter, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(string.IsNullOrWhiteSpace(parameter), message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="parameters"/> is <c>null</c> or contains no elements.
        /// </summary>
        public T AddErrorIfNullOrEmpty<TSource>(IEnumerable<TSource> parameters, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(parameters.IsNullOrEmpty(), message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="parameter"/> equals zero.
        /// </summary>
        public T AddErrorIfZero(double parameter, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(parameter == 0, message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="parameter"/> is strictly negative.
        /// </summary>
        public T AddErrorIfNegative(double parameter, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(parameter < 0, message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when <paramref name="parameter"/> is negative or zero.
        /// </summary>
        public T AddErrorIfNegativeOrZero(double parameter, string message, StatusCode statusCode = StatusCode.BadRequest) => result.AddErrorIf(parameter <= 0, message, statusCode);

        /// <summary>
        /// Validates each element of <paramref name="parameters"/> and appends <paramref name="message"/> when any value is negative or zero,
        /// or when the list itself is <c>null</c> or empty.
        /// </summary>
        public T AddErrorIfNegativeOrZero(List<double> parameters, string message, StatusCode statusCode = StatusCode.BadRequest)
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
        public T AddErrorIfInvalidEnum<TEnum>(TEnum value, string message, StatusCode statusCode = StatusCode.BadRequest) where TEnum : struct, Enum
            => result.AddErrorIf(!Enum.IsDefined(value), message, statusCode);

        /// <summary>
        /// Appends <paramref name="message"/> when the file does not exist or when <paramref name="fullFileName"/> is blank.
        /// </summary>
        public T AddErrorIfFileNotExist(string fullFileName, string message, StatusCode statusCode = StatusCode.BadRequest)
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
        public T AddErrorIfFileExist(string fullFileName, StatusCode statusCode = StatusCode.BadRequest)
        {
            FileInfo fileInfo = new(fullFileName);
            return result.AddErrorIf(fileInfo.Exists, $"File '{fullFileName}' already exists.", statusCode);
        }

        /// <summary>
        /// Appends an error when the directory does not exist or when <paramref name="fullDirectoryName"/> is blank.
        /// </summary>
        public T AddErrorIfDirectoryNotExist(string fullDirectoryName, string parameterName, StatusCode statusCode = StatusCode.BadRequest)
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
        public JsonResult BuildHttpResponse() => new(result) { StatusCode = (int)result.StatusCode };

        /// <summary>
        /// Projects the operation result into an <see cref="IResult"/> suitable for minimal-API endpoints,
        /// preserving the result payload and HTTP status code.
        /// </summary>
        /// <returns>An <see cref="IResult"/> that serializes <paramref name="result"/> as JSON with the result status code.</returns>
        public IResult ToHttpResult() => Results.Json(result, statusCode: (int)result.StatusCode);

        public TOut Match<TOut>(Func<TOut> onSuccess, Func<T, TOut> onError)
        where TOut : Result
        => result.Success ? onSuccess() : onError(result);

        public Task<TOut> Match<TOut>(Func<Task<TOut>> onSuccess, Func<T, TOut> onError)
            where TOut : Result
            => result is null || result.Success ? onSuccess() : Task.FromResult(onError(result));
    }

    /// <summary>
    /// Extension methods for <see cref="Task{TResult}"/> where <typeparamref name="TResult"/> is a <see cref="ResultBase"/>, allowing to project asynchronous operation results into HTTP responses.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="resultTask">The task containing the operation result to be returned.</param>
    extension<TResult>(Task<TResult> resultTask) where TResult : ResultBase
    {
        /// <summary>
        /// Awaits the result task and projects the result into a <see cref="JsonResult"/> carrying the same HTTP status code.
        /// </summary>
        /// <returns>A <see cref="JsonResult"/> that serializes the operation result with the result status code.</returns>
        public async Task<JsonResult> BuildHttpResponseAsync()
        {
            var result = await resultTask.ConfigureAwait(false);
            return result.BuildHttpResponse();
        }

        /// <summary>
        /// Projects the operation result into an <see cref="IResult"/> suitable for minimal-API endpoints,
        /// preserving the result payload and HTTP status code.
        /// </summary>
        /// <returns>An <see cref="IResult"/> that serializes operation result as JSON with the result status code.</returns>
        public async Task<IResult> ToHttpResultAsync()
        {
            var result = await resultTask.ConfigureAwait(false);
            return result.ToHttpResult();
        }
    }
}
