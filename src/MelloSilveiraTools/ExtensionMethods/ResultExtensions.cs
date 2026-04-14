using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.ExtensionMethods;

public static class ResultExtensions
{
    public static async Task<IResult> ToOkResultAsync(this Task operation)
    {
        await operation.ConfigureAwait(false);
        return Results.Ok();
    }

    public static async Task<IResult> ToOkResultAsync<T>(this Task<T> operation)
    {
        var responseData = await operation.ConfigureAwait(false);
        return Results.Ok(responseData);
    }

    public static async Task<IResult> ToCreatedResultAsync(this Task operation)
    {
        await operation.ConfigureAwait(false);
        return Results.Created();
    }

    public static async Task<IResult> ToCreatedResultAsync<T>(this Task<T> operation, string uri = "")
    {
        var responseData = await operation.ConfigureAwait(false);
        return Results.Created(uri, responseData);
    }

    public static Task<IResult> ToNdjsonResultAsync<T>(this IAsyncEnumerable<T> data)
        => Task.FromResult(Results.Extensions.Ndjson(data));
}
