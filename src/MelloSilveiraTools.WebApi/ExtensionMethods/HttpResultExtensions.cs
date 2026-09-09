using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Provides extension methods that convert operation <see cref="Task"/> results into minimal API <see cref="IResult"/> values.
/// </summary>
public static class HttpResultExtensions
{
    extension(Task operation)
    {
        /// <summary>
        /// Awaits the operation and returns an empty HTTP 200 OK result.
        /// </summary>
        public async Task<IResult> ToOkResultAsync()
        {
            await operation.ConfigureAwait(false);
            return Results.Ok();
        }

        /// <summary>
        /// Awaits the operation and returns an empty HTTP 201 Created result.
        /// </summary>
        public async Task<IResult> ToCreatedResultAsync()
        {
            await operation.ConfigureAwait(false);
            return Results.Created();
        }
    }

    extension<T>(Task<T> operation)
    {
        /// <summary>
        /// Awaits the operation and returns an HTTP 200 OK result wrapping the produced value.
        /// </summary>
        public async Task<IResult> ToOkResultAsync()
        {
            var responseData = await operation.ConfigureAwait(false);
            return Results.Ok(responseData);
        }

        /// <summary>
        /// Awaits the operation and returns an HTTP 201 Created result pointing at <paramref name="uri"/> and wrapping the produced value.
        /// </summary>
        public async Task<IResult> ToCreatedResultAsync(string uri = "")
        {
            var responseData = await operation.ConfigureAwait(false);
            return Results.Created(uri, responseData);
        }
    }

    extension<T>(IAsyncEnumerable<T> data)
    {
        /// <summary>
        /// Wraps the supplied asynchronous sequence in an NDJSON streaming result.
        /// </summary>
        public Task<IResult> ToNdjsonResultAsync()
            => Task.FromResult(Results.Extensions.Ndjson(data));
    }
}
