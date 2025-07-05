using MelloSilveiraTools.UseCases.Operations;
using System.Net;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="OperationResponse"/>.
/// </summary>
public static class OperationResponseExtensions
{
    public static T AddErrorIf<T>(this T response, bool condition, string errorMessage, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        if (condition)
        {
            response.ErrorMessages.Add(errorMessage);
            response.SetStatusCode(httpStatusCode);
        }

        return response;
    }

    public static T AddErrorIfNull<T>(this T response, object value, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(value is null, message, httpStatusCode);
    }

    public static T AddErrorIfNotNull<T>(this T response, object value, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(value is not null, message, httpStatusCode);
    }

    public static T AddErrorIfNullOrEmpty<T>(this T response, string parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(string.IsNullOrEmpty(parameter), message, httpStatusCode);
    }

    public static T AddErrorIfNullOrWhiteSpace<T>(this T response, string parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(string.IsNullOrWhiteSpace(parameter), message, httpStatusCode);
    }

    public static T AddErrorIfNullOrEmpty<T, TSource>(this T response, IEnumerable<TSource> parameters, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(parameters.IsNullOrEmpty(), message, httpStatusCode);
    }

    public static T AddErrorIfZero<T>(this T response, double parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(parameter == 0, message, httpStatusCode);
    }

    public static T AddErrorIfNegative<T>(this T response, double parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(parameter < 0, message, httpStatusCode);
    }

    public static T AddErrorIfNegativeOrZero<T>(this T response, double parameter, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        return response.AddErrorIf(parameter <= 0, message, httpStatusCode);
    }

    public static T AddErrorIfNegativeOrZero<T>(this T response, List<double> parameters, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
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

    public static T AddErrorIfInvalidEnum<T, TEnum>(this T response, TEnum value, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
        where TEnum : struct, Enum
    {
        return response.AddErrorIf(!Enum.IsDefined(value), message, httpStatusCode);
    }

    public static T AddErrorIfFileNotExist<T>(this T response, string fullFileName, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        response.AddErrorIfNullOrWhiteSpace(fullFileName, message, httpStatusCode);
        if (!response.Success)
            return response;

        FileInfo fileInfo = new(fullFileName);
        return response.AddErrorIf(!fileInfo.Exists, message, httpStatusCode);
    }

    public static T AddErrorIfFileExist<T>(this T response, string fullFileName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        FileInfo fileInfo = new(fullFileName);
        return response.AddErrorIf(fileInfo.Exists, $"File '{fullFileName}' already exists.", httpStatusCode);
    }

    public static T AddErrorIfDirectoryNotExist<T>(this T response, string fullDirectoryName, string parameterName, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest) where T : OperationResponse
    {
        response.AddErrorIfNullOrEmpty(fullDirectoryName, parameterName, httpStatusCode);
        if (!response.Success)
            return response;

        DirectoryInfo directoryInfo = new(fullDirectoryName);
        return response.AddErrorIf(!directoryInfo.Exists, $"Directory '{fullDirectoryName}' does not exist.", httpStatusCode);
    }
}
