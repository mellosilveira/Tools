using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="HttpResponse"/>.
/// </summary>
public static class HttpResponseExtensions
{
    private const string NdJsonNewLine = "\n";

    private static readonly JsonSerializerOptions _fallbackNdJsonOptions = CreateOptionsWithStringEnums(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _cachedEnumSafeOptions = new();

    extension(HttpResponse response)
    {
        /// <summary>
        /// Serializes the supplied object as JSON, writes it to the response followed by a newline delimiter (NDJSON),
        /// and flushes the response body so the caller receives the chunk immediately.
        /// </summary>
        public async Task<HttpResponse> WriteLineAsNdJsonAsync<T>(T obj, CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions? configuredOptions = response.HttpContext?.RequestServices?.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions;
            JsonSerializerOptions serializerOptions = ResolveNdJsonSerializerOptions(configuredOptions);
            await response.WriteAsync(JsonSerializer.Serialize(obj, serializerOptions) + NdJsonNewLine, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
            return response;
        }
    }

    private static JsonSerializerOptions ResolveNdJsonSerializerOptions(JsonSerializerOptions? configuredOptions)
    {
        if (configuredOptions is null)
            return _fallbackNdJsonOptions;

        if (HasStringEnumConverter(configuredOptions))
            return configuredOptions;

        return _cachedEnumSafeOptions.GetValue(configuredOptions, CreateOptionsWithStringEnums);
    }

    private static bool HasStringEnumConverter(JsonSerializerOptions options)
    {
        foreach (JsonConverter converter in options.Converters)
        {
            if (converter is JsonStringEnumConverter)
                return true;
        }

        return false;
    }

    private static JsonSerializerOptions CreateOptionsWithStringEnums(JsonSerializerOptions source)
    {
        JsonSerializerOptions clone = new(source);
        clone.Converters.Add(new JsonStringEnumConverter());
        return clone;
    }
}
