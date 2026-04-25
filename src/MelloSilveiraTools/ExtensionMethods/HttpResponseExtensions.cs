using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="HttpResponse"/>.
/// </summary>
public static class HttpResponseExtensions
{
    private const string NdJsonNewLine = "\n";

    /// <summary>
    /// Serializes the supplied object as JSON, writes it to the response followed by a newline delimiter (NDJSON),
    /// and flushes the response body so the caller receives the chunk immediately.
    /// </summary>
    public static async Task<HttpResponse> WriteLineAsNdJsonAsync<T>(this HttpResponse response, T obj, CancellationToken cancellationToken = default)
    {
        await response.WriteAsync(JsonSerializer.Serialize(obj) + NdJsonNewLine, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
        return response;
    }
}
