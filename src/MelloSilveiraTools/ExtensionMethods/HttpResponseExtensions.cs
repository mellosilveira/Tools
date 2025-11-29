using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MelloSilveiraTools.ExtensionMethods;

public static class HttpResponseExtensions
{
    private const string NdJsonNewLine = "\n";

    public static async Task<HttpResponse> WriteLineAsNdJsonAsync<T>(this HttpResponse response, T obj, CancellationToken cancellationToken = default)
    {
        await response.WriteAsync(JsonSerializer.Serialize(obj) + NdJsonNewLine, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
        return response;
    }
}
