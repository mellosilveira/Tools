using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Custom <see cref="JsonConverter{T}"/> enabling serialization and deserialization
/// of <see cref="Type"/> instances and dictionary keys using assembly-qualified names.
/// </summary>
public sealed class TypeJsonConverter : JsonConverter<Type>
{
    /// <inheritdoc/>
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? typeName = reader.GetString();
        return ResolveType(typeName);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AssemblyQualifiedName ?? value.FullName ?? value.Name);
    }

    /// <inheritdoc/>
    public override Type ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? typeName = reader.GetString();
        return ResolveType(typeName) ?? typeof(object);
    }

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.AssemblyQualifiedName ?? value.FullName ?? value.Name);
    }

    private static Type? ResolveType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        Type? type = Type.GetType(typeName);
        if (type is not null)
            return type;

        string simpleName = typeName.Contains(',') ? typeName[..typeName.IndexOf(',')].Trim() : typeName;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(simpleName);
            if (type is not null)
                return type;
        }

        return typeof(object);
    }
}
