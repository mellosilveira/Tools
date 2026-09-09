using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="HttpResponse"/>.
/// </summary>
public static class HttpResponseExtensions
{
    private const string NdJsonNewLine = "\n";

    extension(HttpResponse response)
    {
        /// <summary>
        /// Serializes the supplied object as JSON, writes it to the response followed by a newline delimiter (NDJSON),
        /// and flushes the response body so the caller receives the chunk immediately.
        /// </summary>
        public async Task<HttpResponse> WriteLineAsNdJsonAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            await response.WriteAsync(JsonSerializer.Serialize(obj) + NdJsonNewLine, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
            return response;
        }
    }
}
