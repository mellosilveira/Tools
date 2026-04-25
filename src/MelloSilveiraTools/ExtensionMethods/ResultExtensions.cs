using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Provides extension methods that convert operation <see cref="Task"/> results into minimal API <see cref="IResult"/> values.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Awaits the operation and returns an empty HTTP 200 OK result.
    /// </summary>
    public static async Task<IResult> ToOkResultAsync(this Task operation)
    {
        await operation.ConfigureAwait(false);
        return Results.Ok();
    }

    /// <summary>
    /// Awaits the operation and returns an HTTP 200 OK result wrapping the produced value.
    /// </summary>
    public static async Task<IResult> ToOkResultAsync<T>(this Task<T> operation)
    {
        var responseData = await operation.ConfigureAwait(false);
        return Results.Ok(responseData);
    }

    /// <summary>
    /// Awaits the operation and returns an empty HTTP 201 Created result.
    /// </summary>
    public static async Task<IResult> ToCreatedResultAsync(this Task operation)
    {
        await operation.ConfigureAwait(false);
        return Results.Created();
    }

    /// <summary>
    /// Awaits the operation and returns an HTTP 201 Created result pointing at <paramref name="uri"/> and wrapping the produced value.
    /// </summary>
    public static async Task<IResult> ToCreatedResultAsync<T>(this Task<T> operation, string uri = "")
    {
        var responseData = await operation.ConfigureAwait(false);
        return Results.Created(uri, responseData);
    }

    /// <summary>
    /// Wraps the supplied asynchronous sequence in an NDJSON streaming result.
    /// </summary>
    public static Task<IResult> ToNdjsonResultAsync<T>(this IAsyncEnumerable<T> data)
        => Task.FromResult(Results.Extensions.Ndjson(data));
}
