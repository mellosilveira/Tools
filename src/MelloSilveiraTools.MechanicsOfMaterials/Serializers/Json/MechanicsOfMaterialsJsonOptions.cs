using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MelloSilveiraTools.MechanicsOfMaterials.Serializers.Json;

/// <summary>
/// Centralized JSON serialization and schema-export options for <c>MelloSilveiraTools.MechanicsOfMaterials</c>.
/// </summary>
internal static class MechanicsOfMaterialsJsonOptions
{
    /// <summary>
    /// Shared web-default JSON serialization options configured with case-insensitive property matching,
    /// <see cref="DefaultJsonTypeInfoResolver"/> (for JSON schema export), and <see cref="JsonStringEnumConverter"/>.
    /// </summary>
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Exports the JSON Schema representation of <paramref name="type"/> using <see cref="Default"/> options.
    /// </summary>
    internal static JsonNode GetJsonSchemaAsNode(Type type) => JsonSchemaExporter.GetJsonSchemaAsNode(Default, type);

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> into the specified <paramref name="returnType"/> using <see cref="Default"/> options.
    /// </summary>
    internal static T? SafeDeserialize<T>(ILogger logger, JsonElement element, Type returnType)
    {
        try
        {
            return (T?)element.Deserialize(returnType, Default);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize JSON element into type. {@ReturnType}.", returnType);
            return default;
        }
    }

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> into the specified <paramref name="returnType"/> using <see cref="Default"/> options.
    /// </summary>
    internal static object? Deserialize(JsonElement element, Type returnType) => element.Deserialize(returnType, Default);

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> into the specified <typeparamref name="T"/> using <see cref="Default"/> options.
    /// </summary>
    internal static object? Deserialize<T>(JsonElement element) => element.Deserialize<T>(Default);
}
